namespace LMS.BuildingBlocks.Core.Authorization;

// LMS 本地签发的司机/客户访问令牌（LmsAppBearer）使用的声明名。签发方（Platform）与各验证方共用同一份常量。
public static class AppTokenClaims
{
    public const string Subject = "sub";

    // 当前登录入口固定的主体类型：DRIVER / CUSTOMER；服务间调用为 SERVICE。
    public const string PrincipalType = "principalType";

    public const string Roles = "roles";

    // 空格分隔的操作范围；业务层仍核对权限和数据归属。
    public const string Scope = "scope";

    // 刷新链标识（familyId），仅用于退出与审计关联。
    public const string SessionId = "sid";

    public const string TokenId = "jti";
}
