using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.SelfService.Models;

// 客户视角的取消能力与政策。BlockedReason 在不可取消时给出：STATE_NOT_CANCELLABLE / WITHIN_CANCELLATION_CUTOFF。
public sealed record SelfCancellationPolicy(Trip Trip, bool CanCancel, string? BlockedReason, string? PolicyReference, string ReasonCode);
