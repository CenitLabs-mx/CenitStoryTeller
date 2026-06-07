using System.Security.Claims;
using CenitStoryTeller.Data;

namespace CenitStoryTeller.Web;

// Lee el Id del usuario autenticado desde el ClaimsPrincipal del HttpContext.
// Si no hay HttpContext (background, console) o no hay usuario, devuelve null.
public sealed class HttpCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _http;
    public HttpCurrentUser(IHttpContextAccessor http) => _http = http;

    public Guid? Id
    {
        get
        {
            var user = _http.HttpContext?.User;
            if (user?.Identity is null || !user.Identity.IsAuthenticated) return null;
            var sub = user.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }
}
