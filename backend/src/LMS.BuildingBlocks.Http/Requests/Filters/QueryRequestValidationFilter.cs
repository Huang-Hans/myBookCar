using LMS.BuildingBlocks.Http.Requests.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LMS.BuildingBlocks.Http.Requests.Filters;

internal sealed class QueryRequestValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var failure = context.ActionArguments.Values.OfType<QueryRequest>()
            .Select(request => request.BindingFailure).FirstOrDefault(result => result is not null);
        if (failure is not null)
        {
            context.Result = new QueryFailureActionResult(failure);
            return;
        }

        await next();
    }

    private sealed class QueryFailureActionResult(IResult result) : IActionResult
    {
        public Task ExecuteResultAsync(ActionContext context) => result.ExecuteAsync(context.HttpContext);
    }
}
