using System.ComponentModel.DataAnnotations;
using LMS.BuildingBlocks.Core.Querying;

namespace LMS.BuildingBlocks.Http.Requests.Validation;

public sealed class PageSizeAttribute : ValidationAttribute
{
    public IReadOnlyList<int> AllowedValues => PageSizes.Allowed;

    public override bool IsValid(object? value) => value is int size && AllowedValues.Contains(size);

    public override string FormatErrorMessage(string name) =>
        $"Must be one of: {string.Join(", ", AllowedValues)}";
}
