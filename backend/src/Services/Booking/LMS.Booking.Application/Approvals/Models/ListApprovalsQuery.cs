using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Booking.Domain.Approvals;

namespace LMS.Booking.Application.Approvals.Models;

// 列表与汇总共用的授权范围和筛选。From 含，To 不含，均按取货时间。
public sealed record ListApprovalsQuery(
    ListQuery List,
    PropertyScope Scope,
    string? PropertyId,
    ApprovalStatus? Status,
    DateTimeOffset? From,
    DateTimeOffset? To);
