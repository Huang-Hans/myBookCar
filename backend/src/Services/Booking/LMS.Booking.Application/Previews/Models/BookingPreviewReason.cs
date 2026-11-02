namespace LMS.Booking.Application.Previews.Models;

// 不能提交的一条原因：Code 稳定、Field 指向输入字段（空表示整体）、Message 为可展示英文。
public sealed record BookingPreviewReason(string Code, string Field, string Message);
