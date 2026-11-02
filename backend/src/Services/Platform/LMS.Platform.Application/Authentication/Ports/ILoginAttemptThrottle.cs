namespace LMS.Platform.Application.Authentication.Ports;

// 按登录标识限制连续失败次数，抵御口令猜测。
public interface ILoginAttemptThrottle
{
    bool IsBlocked(string key);

    void RecordFailure(string key);

    void Reset(string key);
}
