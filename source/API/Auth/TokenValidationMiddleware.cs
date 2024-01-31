using System.IdentityModel.Tokens.Jwt;

namespace Saitynai2.Auth
{
    public class TokenValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly TokenService _tokenService;

        public TokenValidationMiddleware(RequestDelegate next, TokenService tokenService)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var accessToken = httpContext.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(accessToken) && accessToken.StartsWith("Bearer "))
            {
                accessToken = accessToken.Substring("Bearer ".Length).Trim();
                if (IsTokenExpired(accessToken) || IsTokenRevoked(accessToken))
                {
                    httpContext.Response.StatusCode = 401; // Unauthorized
                    await httpContext.Response.WriteAsync("Access token is invalid or expired.");
                    return;
                }
            }

            await _next(httpContext);
        }


        private bool IsTokenExpired(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            if (handler.CanReadToken(token))
            {
                var jsonToken = handler.ReadToken(token) as JwtSecurityToken;
                if (jsonToken != null)
                {
                    var expirationTime = jsonToken.ValidTo;
                    return expirationTime != null && expirationTime <= DateTime.UtcNow;
                }
            }
            return true; // If unable to read the token or no expiration claim, consider it expired
        }

        private bool IsTokenRevoked(string token)
        {
            return _tokenService.IsTokenRevoked(token);
        }
    }
}
