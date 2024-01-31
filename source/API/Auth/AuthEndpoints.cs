using System.Security.Claims;
using Saitynai2.Auth.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.JsonWebTokens;
using Saitynai2.Helpers;
using Saitynai2.Data;

namespace Saitynai2.Auth
{
    public static class AuthEndpoints
    {
        public static void AddAuthAPI(this WebApplication app)
        {

            // logout
            app.MapPost("api/logout", async (SiteDbContext dbContext, TokenService tokenService, HttpContext httpContext, UserManager<SiteRestUser> userManager, LogoutDTO logoutDto) =>
            {
                // check user exists
                var user = await userManager.FindByNameAsync(logoutDto.Username);
                if (user == null)
                {
                    return Results.Forbid();
                }

                var accessToken = httpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                if (string.IsNullOrEmpty(accessToken))
                {
                    return Results.Unauthorized();
                }

                tokenService.RevokeToken(accessToken);

                user.ForceLogin = true;

                await userManager.UpdateAsync(user);

                return Results.Ok(new SuccessfulLogoutDTO(true));
            });

            // register
            app.MapPost("api/register", async (UserManager<SiteRestUser> userManager, RegisterUserDTO registerUserDto) =>
            {
                // check user exists
                var user = await userManager.FindByNameAsync(registerUserDto.Username);
                if (user != null)
                {
                    return Results.UnprocessableEntity("Username already taken");
                }

                var newUser = new SiteRestUser
                {
                    Email = registerUserDto.Email,
                    UserName = registerUserDto.Username
                };


                var createUserResult = await userManager.CreateAsync(newUser, registerUserDto.Password);
                if(!createUserResult.Succeeded)
                {
                    Object returnErrors = ErrorStack.CreateErrorStackFromIdentityErrors(createUserResult.Errors);
                    return Results.UnprocessableEntity(returnErrors);
                }

                await userManager.AddToRoleAsync(newUser, SiteRoles.SiteUser);

                return Results.Created("api/login", new UserDTO(newUser.Id, newUser.UserName, newUser.Email));
            });

            // login
            app.MapPost("api/login", async (UserManager<SiteRestUser> userManager, TokenService tokenService, LoginUserDTO loginUserDto) =>
            {
                // check user exists
                var user = await userManager.FindByNameAsync(loginUserDto.Username);
                if (user == null)
                {
                    return Results.UnprocessableEntity("Username and/or password incorrect.");
                }

                var isPasswordValid = await userManager.CheckPasswordAsync(user, loginUserDto.Password);
                if (!isPasswordValid)
                {
                    return Results.UnprocessableEntity("Username and/or password incorrect.");
                }

                user.ForceLogin = false;

                await userManager.UpdateAsync(user);
                

                var roles = await userManager.GetRolesAsync(user);

                var token = tokenService.CreateAccessToken(user.UserName, user.Id, roles);
                var refreshToken = tokenService.CreateRefreshToken(user.Id);

                return Results.Ok(new SuccessfulLoginDTO(token, refreshToken));
            });


            // accessToken
            app.MapPost("api/accessToken", async (UserManager<SiteRestUser> userManager, TokenService tokenService, RefreshAccessDTO refreshAccessDto) =>
            {
                if(!tokenService.TryParseRefreshToken(refreshAccessDto.refreshToken, out var claims))
                {
                    return Results.UnprocessableEntity();
                }

                var userId = claims.FindFirstValue(JwtRegisteredClaimNames.Sub);

                var user = await userManager.FindByIdAsync(userId);
                if(user == null)
                {
                    return Results.UnprocessableEntity("Invalid token");
                }

                if(user.ForceLogin)
                {
                    return Results.UnprocessableEntity("You must login.");
                }

                var roles = await userManager.GetRolesAsync(user);

                var accessToken = tokenService.CreateAccessToken(user.UserName, user.Id, roles);
                var refreshToken = tokenService.CreateRefreshToken(user.Id);

                return Results.Ok(new SuccessfulLoginDTO(accessToken, refreshToken));
            });
        }


        public record RegisterUserDTO(string Username, string Email, string Password);
        public record UserDTO(string UserId, string Username, string Email);
        public record LoginUserDTO(string Username, string Password);
        public record SuccessfulLoginDTO(string token, string refreshToken);
        public record RefreshAccessDTO(string refreshToken);
        public record LogoutDTO(string Username);
        public record SuccessfulLogoutDTO(bool loggedOut);
    }
}
