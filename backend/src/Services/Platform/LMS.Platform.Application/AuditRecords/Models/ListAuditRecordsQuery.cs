using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;

namespace LMS.Platform.Application.AuditRecords.Models;

// 范围内查询 Platform 自有的审计记录。From 含，To 不含。
public sealed record ListAuditRecordsQuery(
    ListQuery List,
    PropertyScope Scope,
    string? PropertyId,
    DateTimeOffset? From,
    DateTimeOffset? To);
