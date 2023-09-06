using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace selflix.Extensions;
public class OpenApiEnumSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type.IsEnum)
        {
            var array = new OpenApiArray();
            array.AddRange(Enum.GetNames(context.Type).Select(n => new OpenApiString(n)));
            schema.Extensions.Add("x-enum-varnames", array);
        }
    }
}