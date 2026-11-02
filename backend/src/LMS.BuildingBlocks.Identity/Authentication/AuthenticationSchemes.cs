namespace LMS.BuildingBlocks.Identity.Authentication;

public static class AuthenticationSchemes
{
    // 员工/客服：Keycloak 企业令牌。
    public const string StaffBearer = "StaffBearer";

    // 司机与客户：LMS 本地身份服务签发的令牌。
    public const string LmsAppBearer = "LmsAppBearer";

    // 服务间受限身份：由调用服务用自己的密钥签发，只用于内部接口（例如 Booking 后台解析通知收件人）。
    public const string ServiceBearer = "ServiceBearer";
}
