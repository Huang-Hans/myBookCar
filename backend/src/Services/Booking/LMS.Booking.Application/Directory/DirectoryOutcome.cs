namespace LMS.Booking.Application.Directory;

public enum DirectoryOutcome
{
    Success,
    NotFound,

    // 对方因请求内容不被接受而拒绝（字段问题见 Issues）。
    Rejected,
    Conflict,
    Forbidden,

    // 对方暂时不可用，不能给出真实结果。
    Unavailable
}
