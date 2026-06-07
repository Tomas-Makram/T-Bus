using BusinessLayer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace BusinessLayer.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class EncryptedRoleAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _roles;

        public EncryptedRoleAttribute(params string[] roles)
        {
            _roles = roles;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            if (user.Identity == null || !user.Identity.IsAuthenticated)
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    success = false,
                    message = "Unauthorized"
                });

                return;
            }

            var encryptedRole = user.FindFirstValue(ClaimTypes.Role);

            if (string.IsNullOrWhiteSpace(encryptedRole))
            {
                context.Result = new ForbidResult();
                return;
            }

            var cipher = context.HttpContext.RequestServices.GetRequiredService<IDataCiphers>();

            string role;

            try
            {
                role = cipher.Decrypt(encryptedRole);
            }
            catch
            {
                context.Result = new ForbidResult();
                return;
            }

            var authorized = _roles.Any(r => r.Equals(role, StringComparison.OrdinalIgnoreCase));

            if (!authorized)
            {
                context.Result = new ObjectResult(new
                {
                    success = false,
                    message = "Forbidden"
                })
                {
                    StatusCode = 403
                };
            }
        }
    }
}