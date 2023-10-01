using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using selflix.Db;

namespace selflix.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class Test2Controller : ControllerBase
{
    readonly AppDbContext _db;
    readonly IBackgroundJobClient _job;
    private readonly ILogger<Test2Controller> _logger;

    public Test2Controller(AppDbContext db, IBackgroundJobClient job, ILogger<Test2Controller> logger)
    {
        _db = db;
        _job = job;
        _logger = logger;
    }
    [HttpGet(Name = "test2")]
    public async Task<IActionResult> GetAsync()
    {
        _logger.LogInformation("Starting job");
        var library = await _db.Libraries.FirstOrDefaultAsync();
        if (library == null)
        {
            library = new Library("Movies", "movies");
            _db.Libraries.Add(library);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Created library");
        }
        _job.Enqueue<Jobs.IndexLibrary>(j => j.RunAsync(library.LibraryId, CancellationToken.None));

        _logger.LogInformation("Job started");
        return Ok("Ok");
    }
}