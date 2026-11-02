namespace LMS.Platform.Infrastructure.Configuration.References;

public sealed class ReportingClientOptions
{
    public const string SectionName = "Reporting";
    public string BaseAddress { get; init; } = "http://reporting-api";
}
