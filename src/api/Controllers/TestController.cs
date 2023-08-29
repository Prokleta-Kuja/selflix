using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.Extensions.Primitives;
using System.Net;

namespace selflix.Controllers;

// https://github.com/dotnet/aspnetcore/blob/main/src/Middleware/StaticFiles/src/StaticFileContext.cs

[ApiController]
[Route("[controller]")]
public class TestController : ControllerBase
{
    PreconditionState _ifMatchState = PreconditionState.Unspecified;
    PreconditionState _ifNoneMatchState = PreconditionState.Unspecified;
    PreconditionState _ifModifiedSinceState = PreconditionState.Unspecified;
    PreconditionState _ifUnmodifiedSinceState = PreconditionState.Unspecified;
    private readonly ILogger<TestController> _logger;

    public TestController(ILogger<TestController> logger)
    {
        _logger = logger;
    }

    [HttpGet(Name = "Test")]
    [HttpHead(Name = "TestHead")]
    public async Task GetAsync()
    {
        // TODO: lookup local file
        var fi = new FileInfo("/workspaces/selflix/src/api/content/e.mkv");

        var now = DateTimeOffset.UtcNow;
        var lastModified = new DateTimeOffset(fi.LastWriteTimeUtc);
        var etagHash = lastModified.ToFileTime() ^ fi.Length;
        var etag = new EntityTagHeaderValue($@"""{Convert.ToString(etagHash, 16)}""");
        var requestHeaders = Request.GetTypedHeaders();
        var isRangeRequest = false;

        // 14.24 If-Match
        var ifMatch = requestHeaders.IfMatch;
        if (ifMatch?.Count > 0)
        {
            _ifMatchState = PreconditionState.PreconditionFailed;
            foreach (var reqEtag in ifMatch)
                if (reqEtag.Equals(EntityTagHeaderValue.Any) || reqEtag.Compare(etag, useStrongComparison: true))
                {
                    _ifMatchState = PreconditionState.ShouldProcess;
                    break;
                }
        }

        // 14.26 If-None-Match
        var ifNoneMatch = requestHeaders.IfNoneMatch;
        if (ifNoneMatch?.Count > 0)
        {
            _ifNoneMatchState = PreconditionState.ShouldProcess;
            foreach (var reqEtag in ifNoneMatch)
                if (reqEtag.Equals(EntityTagHeaderValue.Any) || reqEtag.Compare(etag, useStrongComparison: true))
                {
                    _ifNoneMatchState = PreconditionState.NotModified;
                    break;
                }
        }

        // 14.25 If-Modified-Since
        var ifModifiedSince = requestHeaders.IfModifiedSince;
        if (ifModifiedSince.HasValue && ifModifiedSince <= now)
        {
            bool modified = ifModifiedSince < lastModified;
            _ifModifiedSinceState = modified ? PreconditionState.ShouldProcess : PreconditionState.NotModified;
        }

        // 14.28 If-Unmodified-Since
        var ifUnmodifiedSince = requestHeaders.IfUnmodifiedSince;
        if (ifUnmodifiedSince.HasValue && ifUnmodifiedSince <= now)
        {
            bool unmodified = ifUnmodifiedSince >= lastModified;
            _ifUnmodifiedSinceState = unmodified ? PreconditionState.ShouldProcess : PreconditionState.PreconditionFailed;
        }

        // 14.35 Range
        // http://tools.ietf.org/html/draft-ietf-httpbis-p5-range-24

        // A server MUST ignore a Range header field received with a request method other
        // than GET.
        RangeItemHeaderValue? range = null;
        var isHead = HttpMethods.IsHead(Request.Method);
        if (isHead)
        {
            var parsed = RangeHelper.ParseRange(HttpContext, requestHeaders, fi.Length);
            range = parsed.range;
            isRangeRequest = parsed.isRangeRequest;
        }

        // 14.27 If-Range
        var ifRangeHeader = requestHeaders.IfRange;
        if (ifRangeHeader != null)
        {
            // If the validator given in the If-Range header field matches the
            // current validator for the selected representation of the target
            // resource, then the server SHOULD process the Range header field as
            // requested.  If the validator does not match, the server MUST ignore
            // the Range header field.
            if (ifRangeHeader.LastModified.HasValue)
                if (lastModified > ifRangeHeader.LastModified)
                    isRangeRequest = false;
                else if (etag != null && ifRangeHeader.EntityTag != null && !ifRangeHeader.EntityTag.Compare(etag, useStrongComparison: true))
                    isRangeRequest = false;
        }


        var contentType = new MediaTypeHeaderValue(MimeTypes.GetMimeType(fi.Name));
        var responseHeaders = Response.GetTypedHeaders();
        var max = GetPreconditionState();
        switch (max)
        {
            case PreconditionState.Unspecified:
            case PreconditionState.ShouldProcess:
                if (isHead)
                {
                    ApplyResponseHeaders(responseHeaders, lastModified, etag, fi.Length);
                    responseHeaders.ContentType = contentType;
                    Response.StatusCode = StatusCodes.Status200OK;
                    return;
                }

                try
                {
                    if (isRangeRequest)
                    {
                        if (range == null)
                        {
                            responseHeaders.ContentRange = new ContentRangeHeaderValue(fi.Length);
                            Response.StatusCode = StatusCodes.Status416RangeNotSatisfiable;
                            return;
                        }

                        var begin = range.From!.Value;
                        var end = range.To!.Value;
                        var contentLength = end - begin + 1;
                        responseHeaders.ContentRange = new ContentRangeHeaderValue(begin, end, fi.Length);
                        // SetCompressionMode();
                        responseHeaders.ContentType = contentType;
                        Response.StatusCode = StatusCodes.Status206PartialContent;
                        ApplyResponseHeaders(responseHeaders, lastModified, etag);

                        try
                        {
                            await Response.SendFileAsync(fi.FullName, begin, contentLength, HttpContext.RequestAborted);
                            return;
                        }
                        catch (OperationCanceledException)
                        {
                            // Don't throw this exception, it's most likely caused by the client disconnecting.
                        }
                    }

                    ApplyResponseHeaders(responseHeaders, lastModified, etag, fi.Length);
                    responseHeaders.ContentType = contentType;
                    await Response.SendFileAsync(fi.FullName, 0, fi.Length, HttpContext.RequestAborted);
                    return;
                }
                catch (FileNotFoundException)
                {
                    Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }
            case PreconditionState.NotModified:
                ApplyResponseHeaders(responseHeaders, lastModified, etag);
                responseHeaders.ContentType = contentType;
                Response.StatusCode = StatusCodes.Status304NotModified;
                return;
            case PreconditionState.PreconditionFailed:
                Response.StatusCode = StatusCodes.Status412PreconditionFailed;
                return;
            default:
                var exception = new NotImplementedException(GetPreconditionState().ToString());
                throw exception;
        }

        void ApplyResponseHeaders(ResponseHeaders responseHeaders, DateTimeOffset lastModified, EntityTagHeaderValue? etag, long? length = null)
        {
            responseHeaders.LastModified = lastModified;
            responseHeaders.ETag = etag;
            responseHeaders.Headers.AcceptRanges = "bytes";
            // if (!string.IsNullOrEmpty(_contentType))
            //     responseHeaders.ContentType = _contentType;
            if (length.HasValue)
                responseHeaders.ContentLength = length;
        }
    }

    PreconditionState GetPreconditionState() => GetMaxPreconditionState(_ifMatchState, _ifNoneMatchState, _ifModifiedSinceState, _ifUnmodifiedSinceState);
    static PreconditionState GetMaxPreconditionState(params PreconditionState[] states)
    {
        PreconditionState max = PreconditionState.Unspecified;
        for (int i = 0; i < states.Length; i++)
        {
            if (states[i] > max)
            {
                max = states[i];
            }
        }
        return max;
    }
    internal enum PreconditionState : byte
    {
        Unspecified,
        NotModified,
        ShouldProcess,
        PreconditionFailed
    }
}

/// <summary>
/// Provides a parser for the Range Header in an <see cref="HttpContext.Request"/>.
/// </summary>
internal static class RangeHelper
{
    /// <summary>
    /// Returns the normalized form of the requested range if the Range Header in the <see cref="HttpContext.Request"/> is valid.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> associated with the request.</param>
    /// <param name="requestHeaders">The <see cref="RequestHeaders"/> associated with the given <paramref name="context"/>.</param>
    /// <param name="length">The total length of the file representation requested.</param>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    /// <returns>A boolean value which represents if the <paramref name="requestHeaders"/> contain a single valid
    /// range request. A <see cref="RangeItemHeaderValue"/> which represents the normalized form of the
    /// range parsed from the <paramref name="requestHeaders"/> or <c>null</c> if it cannot be normalized.</returns>
    /// <remark>If the Range header exists but cannot be parsed correctly, or if the provided length is 0, then the range request cannot be satisfied (status 416).
    /// This results in (<c>true</c>,<c>null</c>) return values.</remark>
    public static (bool isRangeRequest, RangeItemHeaderValue? range) ParseRange(HttpContext context, RequestHeaders requestHeaders, long length)
    {
        var rawRangeHeader = context.Request.Headers.Range;
        if (StringValues.IsNullOrEmpty(rawRangeHeader))
            return (false, null);

        // Perf: Check for a single entry before parsing it
        if (rawRangeHeader.Count > 1 || (rawRangeHeader[0] ?? string.Empty).Contains(','))
            // The spec allows for multiple ranges but we choose not to support them because the client may request
            // very strange ranges (e.g. each byte separately, overlapping ranges, etc.) that could negatively
            // impact the server. Ignore the header and serve the response normally.
            return (false, null);

        var rangeHeader = requestHeaders.Range;
        if (rangeHeader == null)
            return (false, null);

        var ranges = rangeHeader.Ranges;
        if (ranges == null)
            return (false, null);

        if (ranges.Count == 0)
            return (true, null);

        if (length == 0)
            return (true, null);

        // Normalize the ranges
        var range = NormalizeRange(ranges.Single(), length);

        // Return the single range
        return (true, range);
    }

    // Internal for testing
    internal static RangeItemHeaderValue? NormalizeRange(RangeItemHeaderValue range, long length)
    {
        var start = range.From;
        var end = range.To;

        // X-[Y]
        if (start.HasValue)
        {
            if (start.Value >= length)
                // Not satisfiable, skip/discard.
                return null;
            if (!end.HasValue || end.Value >= length)
                end = length - 1;
        }
        else if (end.HasValue)
        {
            // suffix range "-X" e.g. the last X bytes, resolve
            if (end.Value == 0)
                // Not satisfiable, skip/discard.
                return null;

            var bytes = Math.Min(end.Value, length);
            start = length - bytes;
            end = start + bytes - 1;
        }

        return new RangeItemHeaderValue(start, end);
    }
}