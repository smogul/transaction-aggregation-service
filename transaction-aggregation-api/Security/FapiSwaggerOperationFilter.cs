using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace transaction_aggregation_api.Security
{
    public class FapiSwaggerOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var allowsAnonymous = context.ApiDescription.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any();

            if (allowsAnonymous)
            {
                return;
            }

            operation.Security.Add(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = FapiOptions.PolicyName }
                    },
                    new[] { FapiOptions.Scope }
                }
            });

            operation.Parameters ??= new List<OpenApiParameter>();
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = FapiOptions.InteractionIdHeader,
                In = ParameterLocation.Header,
                Required = false,
                Description = "Optional. A UUID that ties your request to our logs. If you leave it out we make one and send it back.",
                Schema = new OpenApiSchema { Type = "string", Format = "uuid" },
                Example = new OpenApiString("93bac548-d2de-4546-b106-880a5018460d")
            });

            operation.Responses.TryAdd("401", new OpenApiResponse { Description = "No access token, or the token is not valid." });
        }
    }
}
