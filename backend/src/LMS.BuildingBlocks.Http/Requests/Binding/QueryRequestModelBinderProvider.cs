using LMS.BuildingBlocks.Http.Requests.Contracts;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace LMS.BuildingBlocks.Http.Requests.Binding;

internal sealed class QueryRequestModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context) =>
        context.BindingInfo.BindingSource == BindingSource.Query &&
        typeof(QueryRequest).IsAssignableFrom(context.Metadata.ModelType)
            ? new QueryRequestModelBinder()
            : null;
}
