using LMS.BuildingBlocks.Core.Wire;
using LMS.Dispatch.Domain.Inspections;
using LMS.Dispatch.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Dispatch.Infrastructure.Documents;

internal sealed class InspectionAnswerDocument
{
    [BsonElement("itemCode")]
    public string ItemCode { get; init; } = string.Empty;

    [BsonElement("result")]
    public string Result { get; init; } = string.Empty;

    public static InspectionAnswerDocument FromDomain(InspectionAnswer answer) =>
        new() { ItemCode = answer.ItemCode, Result = WireEnum.ToWire(answer.Result) };

    public InspectionAnswer ToDomain() => new(ItemCode, StoredValues.ParseEnum<InspectionResult>(Result, "answers.result"));
}
