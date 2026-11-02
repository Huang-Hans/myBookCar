using System.Text.Json;
using LMS.BuildingBlocks.Core.Wire;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Domain.Inspections;

namespace LMS.Dispatch.Application.Inspections.Commands;

// 司机提交一次完整检查。DriverId 来自令牌；TemplateVersion 为空表示使用当前发布版本。
public sealed record SubmitInspectionCommand(
    string DriverId,
    InspectionPhase Phase,
    int? TemplateVersion,
    IReadOnlyList<InspectionAnswer> Answers,
    string IdempotencyKey,
    CommandContext Context)
{
    // 答案按检查项代码排序后参与指纹，提交顺序不同但内容一致的重放仍视为同一请求。
    public string ToCanonicalForm() =>
        JsonSerializer.Serialize(new object?[]
        {
            DriverId,
            WireEnum.ToWire(Phase),
            TemplateVersion,
            Answers.OrderBy(answer => answer.ItemCode, StringComparer.Ordinal).Select(answer => new[] { answer.ItemCode, WireEnum.ToWire(answer.Result) })
        });
}
