using System.ComponentModel.DataAnnotations;
using System.Reflection;
using LMS.BuildingBlocks.Http.Requests.Contracts;
using LMS.BuildingBlocks.Http.Requests.Metadata;

namespace LMS.BuildingBlocks.Http.Requests.Binding;

internal static class QueryPropertyReader
{
    private static readonly IReadOnlyDictionary<Type, Func<QueryReader, QueryPropertyMetadata, object?>> PrimitiveReaders =
        new Dictionary<Type, Func<QueryReader, QueryPropertyMetadata, object?>>
        {
            [typeof(string)] = ReadText,
            [typeof(int)] = (reader, property) => reader.OptionalInteger(property.Name, IntegerError(property)),
            [typeof(long)] = (reader, property) => reader.OptionalLong(property.Name, "Must be an integer"),
            [typeof(bool)] = (reader, property) => reader.OptionalBool(property.Name),
            [typeof(DateTimeOffset)] = (reader, property) => reader.OptionalUtcInstant(property.Name)
        };

    public static object? Read(QueryReader reader, QueryPropertyMetadata property)
    {
        var type = Nullable.GetUnderlyingType(property.Property.PropertyType) ?? property.Property.PropertyType;
        if (type.IsEnum)
        {
            return typeof(QueryReader).GetMethod(nameof(QueryReader.OptionalEnum))!
                .MakeGenericMethod(type).Invoke(reader, [property.Name]);
        }

        return ReadPrimitive(reader, property, type);
    }

    private static object? ReadPrimitive(QueryReader reader, QueryPropertyMetadata property, Type type)
    {
        if (!PrimitiveReaders.TryGetValue(type, out var read))
        {
            throw new InvalidOperationException($"Unsupported query property type '{type.FullName}'.");
        }

        return read(reader, property);
    }

    private static string? ReadText(QueryReader reader, QueryPropertyMetadata property) =>
        property.Property.IsDefined(typeof(UnrestrictedTextAttribute))
            ? reader.OptionalUnrestrictedText(property.Name)
            : reader.OptionalText(property.Name);

    private static string IntegerError(QueryPropertyMetadata property) =>
        property.Property.GetCustomAttributes<ValidationAttribute>().FirstOrDefault()?.FormatErrorMessage(property.Name)
        ?? "Must be an integer";
}
