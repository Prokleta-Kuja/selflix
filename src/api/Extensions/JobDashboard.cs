using System.Security.Claims;
using Hangfire;
using Hangfire.Dashboard;

namespace selflix.Extensions;

public static class JobDashboard
{
    public static void UseJobDashboard(this IApplicationBuilder app)
    {
        app.UseHangfireDashboard("/jobs", new DashboardOptions
        {
            Authorization = new[] { new HangfireAuthFilter() },
            DashboardTitle = "poshtar jobs",
            DisplayStorageConnectionString = false,
        });
    }
}

public class HangfireAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var ctx = context.GetHttpContext();
        if (!ctx.User.Identity?.IsAuthenticated ?? true)
            return false;

        // TODO: implement isAdmin claim
        return false;
    }
}