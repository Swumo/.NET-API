using Microsoft.AspNetCore.Identity;

namespace Saitynai2.Auth.Model
{
    public class SiteRestUser : IdentityUser
    {
        public bool ForceLogin { get; set; }
    }
}
