using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.Http.Requests;
using LMS.Dispatch.Api.Contracts.Inspections;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Inspections.Commands;
using LMS.Dispatch.Application.Inspections.Models;
using LMS.Dispatch.Domain.Inspections;

namespace LMS.Dispatch.Api.Mapping.Inspections;

internal static class InspectionMapper
{
    public static InspectionTemplateView ToView(InspectionTemplate template) => new(
        WireEnum.ToWire(template.Phase),
        template.Version,
        template.Items.Select(item => new InspectionItemView(item.Code, item.Name)).ToList());

    public static DriverInspectionView ToView(DriverInspection inspection) => new(
        inspection.InspectionId,
        WireEnum.ToWire(inspection.Phase),
        inspection.TemplateVersion,
        inspection.PropertyId,
        inspection.VehicleSnapshot,
        inspection.Answers.Select(answer => new InspectionAnswerView(answer.ItemCode, WireEnum.ToWire(answer.Result))).ToList(),
        inspection.HasFailure,
        inspection.SubmittedAt,
        inspection.Version);

    public static ListInspectionsQuery ToListQuery(DriverInspectionListRequest request, string driverId) =>
        new(request.ToListQuery(), driverId, request.Phase, request.HasFailure, request.From, request.To);

    public static RequestParseResult<SubmitInspectionCommand> ToSubmitCommand(
        string driverId,
        SubmitInspectionRequest request,
        string idempotencyKey,
        CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var phase = errors.RequiredEnum<InspectionPhase>("phase", request.Phase);
        var answers = ReadAnswers(errors, request.Answers);

        return errors.HasErrors
            ? RequestParseResult<SubmitInspectionCommand>.Fail(errors.ToFailure())
            : RequestParseResult<SubmitInspectionCommand>.Success(
                new SubmitInspectionCommand(driverId, phase, request.TemplateVersion, answers, idempotencyKey, context));
    }

    private static List<InspectionAnswer> ReadAnswers(FieldErrorCollector errors, IReadOnlyList<InspectionAnswerRequest>? answers)
    {
        var result = new List<InspectionAnswer>();
        if (answers is null)
        {
            errors.Add("answers", "Is required");
            return result;
        }

        for (var index = 0; index < answers.Count; index++)
        {
            var item = answers[index];
            result.Add(new InspectionAnswer(
                errors.RequiredText($"answers[{index}].itemCode", item?.ItemCode),
                errors.RequiredEnum<InspectionResult>($"answers[{index}].result", item?.Result)));
        }

        return result;
    }
}
