using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Auditing.Services;
using LMS.Booking.Application.Catalog.Ports;
using LMS.Booking.Application.Directory;
using LMS.Booking.Application.Guests.Commands;
using LMS.Booking.Application.Guests.Ports;
using LMS.Booking.Application.Workflows;
using LMS.Booking.Domain.Guests;

namespace LMS.Booking.Application.Guests.Handlers;

internal sealed class UpdateGuestHandler(
    PatchWorkflow workflow,
    IGuestDirectory directory,
    IGuestProfileRepository profiles,
    IPlatformCatalog catalog) : IUpdateGuestHandler
{
    public async Task<OperationResult<GuestProfile>> UpdateAsync(UpdateGuestCommand command, CancellationToken cancellationToken)
    {
        var issues = Validate(command);
        if (issues.Count > 0)
            return OperationResult<GuestProfile>.Invalid(issues);
        var current = await directory.FindAsync(command.GuestId, cancellationToken);
        if (!IsVisible(current, command.Context.Scope))
            return OperationResult<GuestProfile>.Failed(OperationOutcome.NotFound);
        var rejection = await CheckPropertyAsync(command, cancellationToken);
        return rejection ?? await workflow.ExecuteAsync(BuildSpecification(command), cancellationToken);
    }

    private PatchSpecification<GuestProfile> BuildSpecification(UpdateGuestCommand command) => new(
        command.GuestId, 0,
        (id, context, token) => FindVisibleAsync(id, command.Context.Scope, context, token),
        _ => 0,
        (guest, _, _) => Task.FromResult(OperationResult<GuestProfile>.Succeeded(guest with
        {
            Name = command.Name.ValueOr(guest.Name),
            TierCode = command.Tier.ValueOr(guest.TierCode),
            RoomNumber = command.RoomNumber.ValueOrCurrent(guest.RoomNumber),
            PropertyId = command.PropertyId.ValueOrCurrent(guest.PropertyId)
        })),
        (guest, _, context, token) => profiles.UpdateAsync(guest, context, token),
        (before, _, now) => AuditRecordFactory.Updated(
            command.Context, "guest.update", "guest", before.GuestId, now, command.ChangedFields, before.PropertyId));

    private async Task<GuestProfile?> FindVisibleAsync(
        string guestId, PropertyScope scope, ITransactionContext context, CancellationToken cancellationToken)
    {
        var guest = await profiles.FindAsync(guestId, context, cancellationToken);
        return IsVisible(guest, scope) ? guest : null;
    }

    private static bool IsVisible(GuestProfile? guest, PropertyScope scope) =>
        guest is not null && scope.Allows(guest.PropertyId ?? string.Empty);

    private async Task<OperationResult<GuestProfile>?> CheckPropertyAsync(UpdateGuestCommand command, CancellationToken cancellationToken)
    {
        if (!command.PropertyId.IsSet)
            return null;
        if (!command.Context.Scope.Allows(command.PropertyId.Value ?? string.Empty))
            return OperationResult<GuestProfile>.Failed(OperationOutcome.Forbidden);
        return await CheckPropertyExistsAsync(command.PropertyId.Value, cancellationToken);
    }

    private async Task<OperationResult<GuestProfile>?> CheckPropertyExistsAsync(string? propertyId, CancellationToken cancellationToken)
    {
        if (propertyId is null)
            return null;
        var property = await catalog.GetPropertyAsync(propertyId, cancellationToken);
        return property.Outcome switch
        {
            DirectoryOutcome.Success => null,
            DirectoryOutcome.NotFound => OperationResult<GuestProfile>.Invalid([new FieldIssue("propertyId", "Unknown property")]),
            DirectoryOutcome.Forbidden => OperationResult<GuestProfile>.Failed(OperationOutcome.Forbidden),
            _ => OperationResult<GuestProfile>.Failed(OperationOutcome.SourceUnavailable)
        };
    }

    private static IReadOnlyList<FieldIssue> Validate(UpdateGuestCommand command)
    {
        var issues = new List<FieldIssue>();
        if (command.ChangedFields.Count == 0)
            issues.Add(new FieldIssue(string.Empty, "At least one field is required"));
        ValidateLength(issues, "name", command.Name.Value, 100);
        ValidateLength(issues, "roomNumber", command.RoomNumber.Value, 50);
        ValidateLength(issues, "propertyId", command.PropertyId.Value, 100);
        ValidateTier(issues, command);
        return issues;
    }

    private static void ValidateTier(List<FieldIssue> issues, UpdateGuestCommand command)
    {
        if (!command.Tier.IsSet)
            return;
        if (!TierCodes.Ascending.Contains(command.Tier.Value, StringComparer.Ordinal))
            issues.Add(new FieldIssue("tier", "Must be one of: SILVER, GOLD, PLATINUM, DIAMOND, VIP"));
    }

    private static void ValidateLength(List<FieldIssue> issues, string field, string? value, int maximum)
    {
        if (value?.Length > maximum)
            issues.Add(new FieldIssue(field, $"Must not exceed {maximum} characters"));
    }
}
