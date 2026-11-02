using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.Http.Requests.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace LMS.BuildingBlocks.Http.Requests.Metadata;

public static class QueryRequestMetadata
{
    public static IReadOnlyList<QueryPropertyMetadata> Read(QueryRequest defaults) => defaults.GetType()
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(IsBindable)
        .OrderBy(property => property.DeclaringType == typeof(PagedQueryRequest) ? 0 : 1)
        .Select(property => new QueryPropertyMetadata(property, GetName(property),
            property.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty, property.GetValue(defaults)))
        .ToList();

    public static string GetName(PropertyInfo property) => property.GetCustomAttribute<FromQueryAttribute>()?.Name
        ?? JsonNamingPolicy.CamelCase.ConvertName(property.Name);

    public static IReadOnlyCollection<string> EnumValues(Type type) => (IReadOnlyCollection<string>)typeof(WireEnum)
        .GetMethod(nameof(WireEnum.WireNames))!.MakeGenericMethod(type).Invoke(null, null)!;

    public static string EnumValue(Enum value) => (string)typeof(WireEnum)
        .GetMethod(nameof(WireEnum.ToWire))!.MakeGenericMethod(value.GetType()).Invoke(null, [value])!;

    private static bool IsBindable(PropertyInfo property) => property.GetMethod?.IsPublic == true &&
        property.SetMethod?.IsPublic == true && property.GetIndexParameters().Length == 0 &&
        property.GetCustomAttribute<BindNeverAttribute>() is null;
}
