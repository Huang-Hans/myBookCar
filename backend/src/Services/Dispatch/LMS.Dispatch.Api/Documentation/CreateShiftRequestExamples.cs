using LMS.Dispatch.Api.Controllers;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LMS.Dispatch.Api.Documentation;

// 新增班次在 Swagger 里直接给出可套用的请求，避免六个字段都显示成 string。
internal sealed class CreateShiftRequestExamples : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.DeclaringType != typeof(ShiftsController) || context.MethodInfo.Name != "CreateAsync")
        {
            return;
        }

        if (operation.RequestBody?.Content is not { } content
            || !content.TryGetValue("application/json", out var media)
            || media is null)
        {
            return;
        }

        media.Examples = new Dictionary<string, OpenApiExample>
        {
            ["morningOnDuty"] = Example(
                "10月25日早班，当值，首选，Alex Chen，City of Dreams",
                "2026-10-25",
                "driver_01a123d9b78a79fcb6b4193753ec6a90",
                "COD",
                "MORNING",
                "ON_DUTY",
                "PREFERRED"),
            ["afternoonRest"] = Example(
                "10月25日午班，休息，Alex Wong，Studio City",
                "2026-10-25",
                "driver_1b3d5d02da374f07a2968ea7b8c1a997",
                "SC",
                "AFTERNOON",
                "REST",
                "PREFERRED"),
            ["nightLeave"] = Example(
                "10月25日夜班，请假，Daniel Chan，Altira",
                "2026-10-25",
                "driver_5231fcffe342444da07c6b90325b10dd",
                "ALT",
                "NIGHT",
                "LEAVE",
                "PREFERRED")
        };
        media.Example = media.Examples["morningOnDuty"].Value;
    }

    private static OpenApiExample Example(
        string summary,
        string date,
        string driverId,
        string propertyId,
        string period,
        string status,
        string role) =>
        new()
        {
            Summary = summary,
            Value = new OpenApiObject
            {
                ["businessDate"] = new OpenApiString(date),
                ["driverId"] = new OpenApiString(driverId),
                ["propertyId"] = new OpenApiString(propertyId),
                ["period"] = new OpenApiString(period),
                ["status"] = new OpenApiString(status),
                ["role"] = new OpenApiString(role)
            }
        };
}
