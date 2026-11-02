namespace LMS.BuildingBlocks.MongoDB.Options;

public sealed class MongoOptions
{
    public const string SectionName = "Mongo";

    // 机密。通过用户机密、环境变量或密钥管理系统提供，禁止提交到仓库。
    // 每个服务、每种角色（运行、迁移）使用各自独立的账号。
    public string ConnectionString { get; init; } = string.Empty;

    // 服务独占的数据库，例如 lms_platform。
    public string DatabaseName { get; init; } = string.Empty;

    public int MaxConnectionPoolSize { get; init; } = 100;

    public int ServerSelectionTimeoutSeconds { get; init; } = 5;
}
