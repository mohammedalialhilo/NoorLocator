using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using NoorLocator.Application.Common.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace NoorLocator.Api.OpenApi;

public class SwaggerDefaultResponsesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        AddErrorResponse(operation, context, StatusCodes.Status400BadRequest, "The request could not be validated.");
        AddErrorResponse(operation, context, StatusCodes.Status500InternalServerError, "The server encountered an unexpected error.");

        if (RequiresAuthorization(context))
        {
            AddErrorResponse(operation, context, StatusCodes.Status401Unauthorized, "Authentication is required to access this endpoint.");

            if (HasElevatedPolicy(context))
            {
                AddErrorResponse(operation, context, StatusCodes.Status403Forbidden, "The authenticated user does not have permission to access this endpoint.");
            }
        }

        if (HasRouteIdParameter(context))
        {
            AddErrorResponse(operation, context, StatusCodes.Status404NotFound, "The requested resource was not found.");
        }
    }

    private static bool RequiresAuthorization(OperationFilterContext context)
    {
        if (context.MethodInfo.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any())
        {
            return false;
        }

        return context.MethodInfo.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any()
               || context.MethodInfo.DeclaringType?.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any() == true;
    }

    private static bool HasElevatedPolicy(OperationFilterContext context)
    {
        var authorizeAttributes = context.MethodInfo.GetCustomAttributes(true).OfType<AuthorizeAttribute>()
            .Concat(context.MethodInfo.DeclaringType?.GetCustomAttributes(true).OfType<AuthorizeAttribute>() ?? Array.Empty<AuthorizeAttribute>());

        return authorizeAttributes.Any(attribute =>
            !string.IsNullOrWhiteSpace(attribute.Roles) ||
            !string.IsNullOrWhiteSpace(attribute.Policy));
    }

    private static bool HasRouteIdParameter(OperationFilterContext context)
        => context.ApiDescription.ParameterDescriptions.Any(parameter =>
            string.Equals(parameter.Name, "id", StringComparison.OrdinalIgnoreCase));

    private static void AddErrorResponse(OpenApiOperation operation, OperationFilterContext context, int statusCode, string description)
    {
        var key = statusCode.ToString();
        operation.Responses ??= new OpenApiResponses();

        if (operation.Responses.ContainsKey(key))
        {
            return;
        }

        operation.Responses[key] = new OpenApiResponse
        {
            Description = description,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new OpenApiMediaType
                {
                    Schema = context.SchemaGenerator.GenerateSchema(typeof(ApiResponse<ApiErrorDetails>), context.SchemaRepository)
                }
            }
        };
    }
}
