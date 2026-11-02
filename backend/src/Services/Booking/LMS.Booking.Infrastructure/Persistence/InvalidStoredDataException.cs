namespace LMS.Booking.Infrastructure.Persistence;

// 已存储的数据无法映射为领域模型（未知枚举值、日期格式错误等）。这表示数据本身已损坏，而不是请求有误。
internal sealed class InvalidStoredDataException(string message) : Exception(message);
