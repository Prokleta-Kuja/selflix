using Hangfire;
using Hangfire.Storage;

namespace selflix.Extensions;

public static class RecurringJobs
{
    public static void ReregisterRecurringJobs(this IApplicationBuilder app)
    {
        // Track active recurring jobs and add/update
        var activeJobIds = new HashSet<string>();

        // activeJobIds.Add(nameof(CleanupTransactions));
        // RecurringJob.AddOrUpdate<CleanupTransactions>(
        //    nameof(CleanupTransactions),
        //    j => j.Run(null, CancellationToken.None),
        //    "0 4 * * *", // Every day @ 4
        //    C.TZ);

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