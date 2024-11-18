using Microsoft.AspNetCore.Authentication;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Security.Claims;
using System.Threading.Tasks;

namespace JW_Utils.JW_HostedServices.AccountManagement
{
    public class CustomClaimsTransformer : IClaimsTransformation
    {
        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            var identity = principal.Identity as ClaimsIdentity;
            if (identity != null && identity.IsAuthenticated)
            {
                var userName = identity.Name;
                var userGroups = GetUserGroups(userName);

                foreach (var group in userGroups)
                {
                    identity.AddClaim(new Claim("groups", group));
                }
            }

            return Task.FromResult(principal);
        }

        private IEnumerable<string> GetUserGroups(string userName)
        {
            var groups = new List<string>();

            using (var context = new PrincipalContext(ContextType.Domain))
            using (var user = UserPrincipal.FindByIdentity(context, userName))
            {
                if (user != null)
                {
                    var userGroups = user.GetAuthorizationGroups();
                    foreach (var group in userGroups)
                    {
                        groups.Add(group.SamAccountName);
                    }
                }
            }

            return groups;
        }
    }
}