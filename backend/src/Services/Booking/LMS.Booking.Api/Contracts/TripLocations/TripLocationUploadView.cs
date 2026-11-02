namespace LMS.Booking.Api.Contracts.TripLocations;

/// <summary>
/// 上报结果。
/// </summary>
/// <param name="AcceptedSeq">服务端已保存的最大序号；重复补传的旧点不会改变它。</param>
public sealed record TripLocationUploadView(long AcceptedSeq);
