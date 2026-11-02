using LMS.BuildingBlocks.Core.Operations;
using LMS.Dispatch.Application.Shifts.Models;
using LMS.Dispatch.Domain.Shifts;

namespace LMS.Dispatch.Application.Shifts.Services;

internal static class ShiftResults
{
    // 创建工作流会区分首次创建与幂等重放，卡片组装后要保留这个结果分类。
    public static OperationResult<ShiftCard> From(OperationResult<DriverShift> source, ShiftCard card) =>
        source.Outcome switch
        {
            OperationOutcome.Created => OperationResult<ShiftCard>.Created(card),
            OperationOutcome.Replayed => OperationResult<ShiftCard>.Replayed(card),
            _ => OperationResult<ShiftCard>.Succeeded(card)
        };
}
