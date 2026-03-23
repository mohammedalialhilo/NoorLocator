using Microsoft.AspNetCore.Mvc;
using NoorLocator.Application.Common.Models;

namespace NoorLocator.Api.Extensions;

public static class ControllerExtensions
{
    public static ActionResult<ApiResponse<object?>> ToActionResult(this ControllerBase controller, OperationResult result)
    {
        var payload = result.Succeeded
            ? ApiResponse<object?>.SuccessResponse(null, result.Message)
            : ApiResponse<object?>.Failure(result.Message);

        return controller.StatusCode(result.StatusCode, payload);
    }

    public static ActionResult<ApiResponse<T>> ToActionResult<T>(this ControllerBase controller, OperationResult<T> result)
    {
        var payload = result.Succeeded
            ? ApiResponse<T>.SuccessResponse(result.Data, result.Message)
            : ApiResponse<T>.Failure(result.Message, result.Data);

        return controller.StatusCode(result.StatusCode, payload);
    }
}
