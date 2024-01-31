using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace Saitynai2.Auth
{
    public class TokenService
    {
        private readonly SymmetricSecurityKey _authSigningKey;
        private readonly string _issuer;
        private readonly string _audience;
        private static readonly List<RevokedToken> _revokedTokens = new List<RevokedToken>();

        public TokenService(IConfiguration configuration)
        {
            _authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Secret"]));
            _issuer = configuration["Jwt:ValidIssuer"];
            _audience = configuration["Jwt:ValidAudience"];
        }

        public string CreateAccessToken(string username, string userId, IEnumerable<string> roles)
        {
            var authClaims = new List<Claim>()
            {
                new (ClaimTypes.Name, username),
                new (JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new (JwtRegisteredClaimNames.Sub, userId)
            };

            authClaims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var token = new JwtSecurityToken
            (
                issuer: _issuer, 
                audience: _audience, 
                expires: DateTime.UtcNow.AddMinutes(10),
                claims: authClaims,
                signingCredentials: new SigningCredentials(_authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        public string CreateRefreshToken(string userId)
        {
            var authClaims = new List<Claim>()
            {
                new (JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new (JwtRegisteredClaimNames.Sub, userId)
            };

            var token = new JwtSecurityToken
            (
                issuer: _issuer,
                audience: _audience,
                expires: DateTime.UtcNow.AddHours(24),
                claims: authClaims,
                signingCredentials: new SigningCredentials(_authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        public bool TryParseRefreshToken(string refreshToken, out ClaimsPrincipal? claims)
        {
            claims = null;

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();

                var validationParams = new TokenValidationParameters
                {
                    ValidIssuer = _issuer,
                    ValidAudience = _audience,
                    IssuerSigningKey = _authSigningKey,
                    ValidateLifetime = true
                };

                claims = tokenHandler.ValidateToken(refreshToken, validationParams, out _);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public class RevokedToken
        {
            public string Token { get; set; }
            public DateTime Expiration { get; set; }
        }

        public void RevokeToken(string token)
        {
            JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
            SecurityToken securityToken = handler.ReadToken(token);
            DateTime expiration = securityToken.ValidTo;
            RevokedToken revokedToken = new RevokedToken() { Token = token, Expiration = expiration };
            _revokedTokens.Add(revokedToken);
        }

        public bool IsTokenRevoked(string token)
        {
            // Remove expired tokens before checking
            CleanupExpiredTokens();
            return _revokedTokens.Any(t => t.Token == token);
        }

        private static void CleanupExpiredTokens()
        {
            var now = DateTime.UtcNow;
            _revokedTokens.RemoveAll(token => token.Expiration <= now);
        }
    }
}
