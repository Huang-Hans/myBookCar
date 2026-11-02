using System.Globalization;
using System.Text.Json.Nodes;
using LMS.ApiGateway.Documentation.Merging;
using Microsoft.OpenApi.Writers;
using Swashbuckle.AspNetCore.Swagger;

namespace LMS.ApiGateway.Documentation.Sources;

// Include local gateway endpoints without making an HTTP request back to the gateway.
internal sealed class GatewayDocumentSource(ISwaggerProvider swaggerProvider)
{
    private static readonly DownstreamDocument Source = new(
        "gateway", "LMS.ApiGateway", "Gateway（网关）", "/swagger/v1/swagger.json", string.Empty);

    public SourceDocument GetDocument()
    {
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        swaggerProvider.GetSwagger("v1").SerializeAsV3(new OpenApiJsonWriter(output));

        return new SourceDocument(Source, (JsonObject)JsonNode.Parse(output.ToString())!);
    }
}
