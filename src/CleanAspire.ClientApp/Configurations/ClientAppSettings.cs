namespace CleanAspire.ClientApp.Configurations;

public class ClientAppSettings
{
    public const string KEY = nameof(ClientAppSettings);
    public string AppName { get; set; } = "Blazor Aspire";
    public string Version { get; set; } = "0.0.1";
    public string ServiceBaseUrl { get; set; } = "http://apiservice:8080";

}

