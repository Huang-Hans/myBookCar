using LMS.Platform.Application.Authentication.Models;

namespace LMS.Platform.Application.Authentication.Ports;

public interface IAppAccessTokenIssuer
{
    // 签名私钥未配置时为 false，登录与刷新返回不可用，不签发任何令牌。
    bool IsAvailable { get; }

    string Issue(AppAccessTokenRequest request);
}
