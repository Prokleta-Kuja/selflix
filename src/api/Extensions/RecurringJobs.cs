using Hangfire;
using Hangfire.Storage;
using selflix.Jobs;

namespace selflix.Extensions;

public static class RecurringJobs
{
    public static void ReregisterRecurringJobs(this IApplicationBuilder app)
    {
        var defaultOpt = new RecurringJobOptions
        {
            MisfireHandling = MisfireHandlingMode.Ignorable,
            TimeZone = C.TZ,
        };
        // Track active recurring jobs and add/update
        var activeJobIds = new HashSet<string>();

        activeJobIds.Add(nameof(ClearAuthTokens));
        RecurringJob.AddOrUpdate<ClearAuthTokens>(
           nameof(ClearAuthTokens),
           j => j.RunAsync(CancellationToken.None),
           "14 * * * *", // Every hour at minute 14
           defaultOpt);

        // activeJobIds.Add(nameof(CertReload));
        // RecurringJob.AddOrUpdate<CertReload>(
        //     nameof(CertReload),
        //     j => j.Run(CancellationToken.None),
        //     "55 3 * * SUN", // Every Sunday @ 3:55
        //     C.TZ);

        // Get all registered recurring jobs
        var conn = JobStorage.Current.GetConnection();
        var jobs = conn.GetRecurringJobs();

        // Unregister recurring jobs no longer active
        foreach (var job in jobs)
            if (!activeJobIds.Contains(job.Id))
                RecurringJob.RemoveIfExists(job.Id);
    }
}