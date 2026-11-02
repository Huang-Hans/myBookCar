using System.Reflection;
using System.ComponentModel.DataAnnotations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Http.Requests.Contracts;
using LMS.BuildingBlocks.Http.Requests.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace LMS.BuildingBlocks.Http.Requests.Binding;

internal sealed class QueryRequestModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext context)
    {
        var model = (QueryRequest)Activator.CreateInstance(context.ModelType)!;
        var reader = QueryReader.For(context.HttpContext);
        BindProperties(reader, model);
        ValidateProperties(reader, model);
        var list = model is PagedQueryRequest paged ? ReadList(reader, paged) : null;
        Complete(model, list, reader);
        context.ValidationState[model] = new ValidationStateEntry { SuppressValidation = true };
        context.Result = ModelBindingResult.Success(model);
        return Task.CompletedTask;
    }

    private static ListQuery ReadList(QueryReader reader, PagedQueryRequest model) =>
        new(model.Page, model.PageSize, model.Search, reader.ParseSort(model.Sort, model.GetSortSpecification()));

    private static void Complete(QueryRequest model, ListQuery? list, QueryReader reader)
    {
        model.BindingFailure = reader.Failure(model.GetType().IsDefined(typeof(AllowRequestBodyAttribute)));
        if (model is PagedQueryRequest paged)
        {
            paged.BindingResult = model.BindingFailure is null
                ? RequestParseResult<ListQuery>.Success(list!)
                : RequestParseResult<ListQuery>.Fail(model.BindingFailure);
        }
    }

    private static void BindProperties(QueryReader reader, QueryRequest model)
    {
        foreach (var property in QueryRequestMetadata.Read(model))
        {
            if (QueryPropertyReader.Read(reader, property) is { } value)
            {
                property.Property.SetValue(model, value);
            }
        }
    }

    private static void ValidateProperties(QueryReader reader, QueryRequest model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        foreach (var result in results)
        {
            foreach (var member in result.MemberNames.DefaultIfEmpty(string.Empty))
            {
                var property = model.GetType().GetProperty(member);
                reader.Reject(property is null ? member : QueryRequestMetadata.GetName(property), result.ErrorMessage!);
            }
        }
    }
}
