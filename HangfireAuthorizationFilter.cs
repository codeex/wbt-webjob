using Hangfire.Dashboard;

namespace WbtWebJob;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // 在生产环境中，应该添加适当的授权逻辑
        // 这里为了开发方便，允许所有访问
        return true;
    }
}
