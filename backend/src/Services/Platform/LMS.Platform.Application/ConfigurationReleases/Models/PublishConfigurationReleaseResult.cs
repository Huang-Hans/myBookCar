using LMS.Platform.Domain.Configuration;

namespace LMS.Platform.Application.ConfigurationReleases.Models;

public sealed record PublishConfigurationReleaseResult(PublishOutcome Outcome, ConfigurationRelease? Release)
{
    public static PublishConfigurationReleaseResult Created(ConfigurationRelease release) => new(PublishOutcome.Created, release);

    public static PublishConfigurationReleaseResult Replayed(ConfigurationRelease release) => new(PublishOutcome.Replayed, release);

    public static PublishConfigurationReleaseResult VersionConflict() => new(PublishOutcome.VersionConflict, null);

    public static PublishConfigurationReleaseResult IdempotencyKeyReused() => new(PublishOutcome.IdempotencyKeyReused, null);
}
