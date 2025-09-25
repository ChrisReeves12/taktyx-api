using Microsoft.AspNetCore.Mvc;

namespace TaktyxAPI.Api.Extensions;

public static class ControllerExtensions
{
    public static int? GetCurrentUserId(this ControllerBase controller)
    {
        var userIdClaim = controller.User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return null;
        }
            
        return userId;
    }
}