namespace MyApp.Server.Options;

public sealed class AppOptions
{
    public string[] CorsOrigins { get; init; } = ["http://localhost:4200"];
}
