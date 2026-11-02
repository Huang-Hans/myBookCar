using System.ComponentModel.DataAnnotations;
using System.Reflection;
using LMS.BuildingBlocks.Http.Requests.Metadata;
using LMS.BuildingBlocks.Http.Requests.Validation;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace LMS.ServiceDefaults.Documentation;

internal static class QueryParameterSchemaFactory
{
    private static readonly IReadOnlyDictionary<Type, (string Type, string? Format)> PrimitiveSchemas =
        new Dictionary<Type, (string Type, string? Format)>
        {
            [typeof(string)] = ("string", null),
            [typeof(int)] = ("integer", "int32"),
            [typeof(long)] = ("integer", "int64"),
            [typeof(bool)] = ("boolean", null),
            [typeof(DateTimeOffset)] = ("string", "date-time")
        };

    private static readonly IReadOnlyDictionary<Type, Func<object, IOpenApiAny>> PrimitiveValues =
        new Dictionary<Type, Func<object, IOpenApiAny>>
        {
            [typeof(int)] = value => new OpenApiInteger((int)value),
            [typeof(long)] = value => new OpenApiLong((long)value),
            [typeof(bool)] = value => new OpenApiBoolean((bool)value)
        };

    public static OpenApiSchema Create(QueryPropertyMetadata metadata, object? defaultValue)
    {
        var type = Nullable.GetUnderlyingType(metadata.Property.PropertyType) ?? metadata.Property.PropertyType;
        var schema = CreateTypeSchema(type);
        schema.Nullable = metadata.Property.GetCustomAttribute<RequiredAttribute>() is null &&
            new NullabilityInfoContext().Create(metadata.Property).ReadState == NullabilityState.Nullable;
        schema.Default = ToOpenApiValue(defaultValue);
        ApplyConstraints(schema, metadata.Property);
        return schema;
    }

    private static OpenApiSchema CreateTypeSchema(Type type)
    {
        if (type.IsEnum)
        {
            return new OpenApiSchema
            {
                Type = "string",
                Enum = QueryRequestMetadata.EnumValues(type).Select(value => (IOpenApiAny)new OpenApiString(value)).ToList()
            };
        }

        return CreatePrimitiveSchema(type);
    }

    private static OpenApiSchema CreatePrimitiveSchema(Type type)
    {
        var definition = PrimitiveSchemas[type];
        return new OpenApiSchema { Type = definition.Type, Format = definition.Format };
    }

    private static void ApplyConstraints(OpenApiSchema schema, PropertyInfo property)
    {
        if (property.GetCustomAttribute<RangeAttribute>() is { } range)
        {
            schema.Minimum = Convert.ToDecimal(range.Minimum, System.Globalization.CultureInfo.InvariantCulture);
            schema.Maximum = Convert.ToDecimal(range.Maximum, System.Globalization.CultureInfo.InvariantCulture);
        }

        if (property.GetCustomAttribute<PageSizeAttribute>() is { } pageSize)
        {
            schema.Enum = pageSize.AllowedValues.Select(value => (IOpenApiAny)new OpenApiInteger(value)).ToList();
        }
    }

    private static IOpenApiAny? ToOpenApiValue(object? value)
    {
        if (value is null)
        {
            return null;
        }

        return value is Enum enumValue
            ? new OpenApiString(QueryRequestMetadata.EnumValue(enumValue))
            : CreatePrimitiveValue(value);
    }

    private static IOpenApiAny CreatePrimitiveValue(object value) => PrimitiveValues.TryGetValue(value.GetType(), out var create)
        ? create(value)
        : new OpenApiString(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture));
}
