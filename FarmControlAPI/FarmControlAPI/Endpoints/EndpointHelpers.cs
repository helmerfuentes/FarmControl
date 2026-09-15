using FarmControlAPI.Application.Common;

namespace FarmControlAPI.Endpoints;

internal static class EndpointHelpers
{
	internal static IResult ToResponse<T>(Result<T> result) =>
		result.IsSuccess
			? Results.Ok(result.Value)
			: Results.BadRequest(new { error = result.Error });

	internal static IResult ToCreated<T>(Result<T> result, string location) where T : class =>
		result.IsSuccess
			? Results.Created(location, result.Value)
			: Results.BadRequest(new { error = result.Error });
}
