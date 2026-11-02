using LMS.Booking.Application.Directory;

namespace LMS.Booking.Application.Notifications.Ports;

// 员工收件人目录。Platform 拥有员工账号、角色、权限与物业范围；Booking 后台以受限服务身份查询，
// 因为客户的 Token 无权读取员工信息，仅靠在线连接也会遗漏离线客服。
// 查询成功可以是零个收件人；查询失败（含服务身份未配置、收件人角色集合未配置）必须与“零收件人”区分，返回 Unavailable。
public interface IStaffNotificationDirectory
{
    Task<DirectoryResult<IReadOnlyList<string>>> ListRecipientsAsync(string eventType, string propertyId, CancellationToken cancellationToken);
}
