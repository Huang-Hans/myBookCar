namespace LMS.Dispatch.Application.Reports.Models;

// 物业运营中心的一个可信联系渠道。Channel 目前为 PHONE；这里只提供联系资料，不表示拨号已接通或运营已受理。
public sealed record OperationsContact(string Name, string Channel, string Value);
