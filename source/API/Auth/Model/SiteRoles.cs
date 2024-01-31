namespace Saitynai2.Auth.Model
{
    public static class SiteRoles
    {
        public const string Admin = nameof(Admin);
        public const string SiteUser = nameof(SiteUser);

        public static readonly IReadOnlyCollection<string> All = new[] { Admin, SiteUser };
    }
}
