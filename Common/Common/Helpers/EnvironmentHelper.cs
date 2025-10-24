namespace Common.Common.Helpers;

public static class EnvironmentHelper
{
    private const string EnvironmentKey = "ASPNETCORE_ENVIRONMENT";

    private const string DevelopmentEnvironmentKey = "Development";

    private const string StagingEnvironmentKey = "Staging";

    private const string ProductionEnvironmentKey = "Production";

    public static string Environment => System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

    public static bool IsDevelopment => Environment.Equals("Development", StringComparison.OrdinalIgnoreCase);

    public static bool IsStaging => Environment.Equals("Staging", StringComparison.OrdinalIgnoreCase);

    public static bool IsProduction => Environment.Equals("Production", StringComparison.OrdinalIgnoreCase);
}
