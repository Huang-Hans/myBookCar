using LMS.Dispatch.Application.Credentials.Models;

namespace LMS.Dispatch.Application.Credentials.Ports;

public interface IDriverCredentialReader
{
    // 登录标识为规范化手机号（见 PhoneNumber），与库内 phone 精确相等。匹配到零条或多于一条都返回 null，不会选取第一条。
    Task<DriverCredential?> FindByPhoneAsync(string phone, CancellationToken cancellationToken);

    Task<DriverCredential?> FindByIdAsync(string driverId, CancellationToken cancellationToken);
}
