using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace webapi.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class CustomClaimRequirementAttribute : AuthorizeAttribute, IAuthorizationFilter
    {
        private readonly string _claimType;
        private readonly string _claimValue;

        public CustomClaimRequirementAttribute(string claimType, string claimValue)
        {
            _claimType = claimType;
            _claimValue = claimValue;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (context.HttpContext.User.Identity is { IsAuthenticated: false })
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            if (context.HttpContext.User.HasClaim(c => c.Type == _claimType && c.Value == _claimValue)) return;
            context.Result = new ForbidResult();
        }
    }
}
