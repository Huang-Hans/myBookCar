using LMS.Dispatch.Api.Controllers;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LMS.Dispatch.Api.Documentation;

// 新增车辆在 Swagger 里给出可套用的请求，覆盖可用、维修中和停用。
internal sealed class CreateVehicleRequestExamples : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.DeclaringType != typeof(VehiclesController) || context.MethodInfo.Name != "CreateAsync")
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
            ["businessSedan"] = Example(
                "City of Dreams，普通轿车，可用，未派车",
                "MB-56-78",
                "Mercedes E-Class",
                "BUSINESS_SEDAN",
                "COD",
                5,
                25000,
                "AVAILABLE",
                null),
            ["available"] = Example(
                "City of Dreams，豪华七人车，可用，未派车",
                "MR-28-66",
                "Toyota Alphard",
                "LUXURY_MPV",
                "COD",
                6,
                42180,
                "AVAILABLE",
                null),
            ["maintenance"] = Example(
                "Morpheus，豪华轿车，维修中",
                "MU-09-21",
                "Mercedes S-Class",
                "LUXURY_SEDAN",
                "GH",
                3,
                19760,
                "MAINTENANCE",
                null),
            ["onTrip"] = Example(
                "Studio City，豪华七人车，行程中，指派 Daniel Chan",
                "MR-16-82",
                "Mercedes V-Class",
                "LUXURY_MPV",
                "SC",
                6,
                28440,
                "ON_TRIP",
                "driver_5231fcffe342444da07c6b90325b10dd")
        };
        media.Example = media.Examples["available"].Value;
    }

    private static OpenApiExample Example(
        string summary,
        string plate,
        string model,
        string grade,
        string propertyId,
        int seats,
        int mileageKm,
        string status,
        string? driverId)
    {
        var value = new OpenApiObject
        {
            ["plate"] = new OpenApiString(plate),
            ["model"] = new OpenApiString(model),
            ["grade"] = new OpenApiString(grade),
            ["propertyId"] = new OpenApiString(propertyId),
            ["seats"] = new OpenApiInteger(seats),
            ["mileageKm"] = new OpenApiInteger(mileageKm),
            ["status"] = new OpenApiString(status)
        };
        if (driverId is not null)
        {
            value["driverId"] = new OpenApiString(driverId);
        }

        return new OpenApiExample { Summary = summary, Value = value };
    }
}
