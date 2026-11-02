using LMS.BuildingBlocks.Core.Scoping;

namespace LMS.Booking.Application.Abstractions;

// 写操作共有的调用上下文。Actor、ActorRole、ActorName、TraceId、Scope 都由接入层从已验证的身份和当前请求取得，不来自请求正文。
public sealed record CommandContext(string Actor, string TraceId, PropertyScope Scope, string? ActorRole = null, string? ActorName = null);
