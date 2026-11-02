using LMS.BuildingBlocks.Core.Operations;
using LMS.Platform.Application.Auditing.Services;
using LMS.Platform.Application.Configuration.Catalogs;
using LMS.Platform.Application.Users.Commands;
using LMS.Platform.Application.Users.Options;
using LMS.Platform.Application.Users.Ports;
using LMS.Platform.Application.Users.Services;
using LMS.Platform.Application.Workflows;
using LMS.Platform.Domain.Users;
using Microsoft.Extensions.Options;

namespace LMS.Platform.Application.Users.Handlers;

// 新增应用用户只登记本系统授权，不创建 IdP 账户；SubjectId 保持为空，用户在关联前不能登录。
internal sealed class CreatePortalUserHandler(
    CreateWorkflow workflow,
    IPortalUserRepository users,
    IPropertyCatalog properties,
    IOptions<PortalUserOptions> options) : ICreatePortalUserHandler
{
    private const string Operation = "portal-user.create";
    private const string ResourceType = "portal-user";

    public async Task<OperationResult<PortalUser>> HandleAsync(CreatePortalUserCommand command, CancellationToken cancellationToken)
    {
        var candidate = PortalUser.Create(
            command.Name,
            command.CorporateEmail,
            command.Role,
            command.HasAllPropertyAccess,
            command.PropertyId,
            command.IsActive,
            options.Value.ProviderCode);

        var rejection = await CheckAsync(candidate, command, cancellationToken);

        return rejection ?? await workflow.ExecuteAsync(BuildSpecification(command, candidate), cancellationToken);
    }

    private async Task<OperationResult<PortalUser>?> CheckAsync(
        PortalUser candidate,
        CreatePortalUserCommand command,
        CancellationToken cancellationToken)
    {
        var evaluated = PortalUserChecks.Evaluate(candidate, command.Context.Scope);

        return evaluated.IsSuccess
            ? await PropertyReferenceCheck.ResolveAsync<PortalUser>(properties, candidate.PropertyId, cancellationToken)
            : evaluated;
    }

    private CreateSpecification<PortalUser> BuildSpecification(CreatePortalUserCommand command, PortalUser candidate) =>
        new(
            IdempotencyScopes.For(command.Context, Operation, command.IdempotencyKey),
            command.ToCanonicalForm(),
            (_, _) => Task.FromResult(candidate),
            user => user.UserId,
            (user, context, token) => users.InsertAsync(user, context, token),
            (id, token) => users.FindByIdAsync(id, token),
            (user, now) => AuditRecordFactory.Created(command.Context, Operation, ResourceType, user.UserId, now, user.PropertyId));
}
