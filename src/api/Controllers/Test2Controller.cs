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
        var hasLibraries = await _db.Libraries.AnyAsync();
        if (!hasLibraries)
        {
            var movies = new Library("Movies", "movies");
            _db.Libraries.Add(movies);
            var tv = new Library("Tv Shows", "tv");
            _db.Libraries.Add(tv);
            var cartoons = new Library("Cartoons", "cartoons");
            _db.Libraries.Add(cartoons);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Created Libraries");
        }

        _job.Enqueue<Jobs.IndexAll>(j => j.RunAsync(CancellationToken.None));
        return Ok("Ok");
    }
}