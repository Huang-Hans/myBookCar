using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace LMS.BuildingBlocks.MongoDB.Options;

// 失败信息只指出配置项名称，不回显连接串的值。
internal sealed partial class MongoOptionsValidator : IValidateOptions<MongoOptions>
{
    private const int MaxPoolSizeLimit = 1000;
    private const int MaxServerSelectionTimeoutSeconds = 60;

    public ValidateOptionsResult Validate(string? name, MongoOptions options)
    {
        var failures = new List<string>();
        AddIfInvalid(failures, IsValidConnectionString(options.ConnectionString), nameof(MongoOptions.ConnectionString),
            "must be a valid mongodb:// or mongodb+srv:// connection string");
        AddIfInvalid(failures, DatabaseNamePattern().IsMatch(options.DatabaseName), nameof(MongoOptions.DatabaseName),
            "must be lowercase letters, digits or underscores and start with a letter");
        AddIfInvalid(failures, options.MaxConnectionPoolSize is >= 1 and <= MaxPoolSizeLimit, nameof(MongoOptions.MaxConnectionPoolSize),
            $"must be between 1 and {MaxPoolSizeLimit}");
        AddIfInvalid(failures, options.ServerSelectionTimeoutSeconds is >= 1 and <= MaxServerSelectionTimeoutSeconds,
            nameof(MongoOptions.ServerSelectionTimeoutSeconds), $"must be between 1 and {MaxServerSelectionTimeoutSeconds}");

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static void AddIfInvalid(List<string> failures, bool isValid, string setting, string requirement)
    {
        if (!isValid)
        {
            failures.Add($"{MongoOptions.SectionName}:{setting} {requirement}.");
        }
    }

    private static bool IsValidConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return false;
        }

        try
        {
            _ = new MongoUrl(connectionString);
            return true;
        }
        catch (Exception exception) when (exception is MongoConfigurationException or ArgumentException)
        {
            return false;
        }
    }

    [GeneratedRegex("^[a-z][a-z0-9_]{0,62}$", RegexOptions.CultureInvariant)]
    private static partial Regex DatabaseNamePattern();
}
