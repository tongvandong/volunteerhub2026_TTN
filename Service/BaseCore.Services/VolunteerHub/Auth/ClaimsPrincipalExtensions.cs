using System.Security.Claims;

namespace BaseCore.Services.VolunteerHub.Auth;

public static class ClaimsPrincipalExtensions
{
    public static int GetRequiredUserId(this ClaimsPrincipal principal)
    {
        var userIdValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId))
        {
            throw new InvalidOperationException("Authenticated user id claim is missing.");
        }

        return userId;
    }
}
