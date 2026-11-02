using System.Reflection;

namespace LMS.BuildingBlocks.Http.Requests.Metadata;

public sealed record QueryPropertyMetadata(PropertyInfo Property, string Name, string Description, object? DefaultValue);
