using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace WebCore.SwaggerFilters;

public class MlfHeaderFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters.Add(new OpenApiParameter()
        {
            Name = "X-LANGUAGE",
            In = ParameterLocation.Header,
            Required = false,
            Example = new OpenApiString("UZ"),
            AllowEmptyValue = true,
            Schema = new OpenApiSchema
            {
                Enum = new List<IOpenApiAny>
                {
                    new OpenApiString("UZ"),
                    new OpenApiString("RU"),
                    new OpenApiString("CYRL"),
                    new OpenApiString("ENG"),
                },
                Default = new OpenApiString("UZ")
            }
        });
    }
}
