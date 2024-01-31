using System.IdentityModel.Tokens.Jwt;
using System.Text;
using FluentValidation;
using Saitynai2.Data;
using Saitynai2.Endpoints;
using Saitynai2.Auth.Model;
using Saitynai2.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;


JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<SiteDbContext>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddTransient<TokenService>();
builder.Services.AddScoped<AuthDbSeeder>();

/// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        corsBuilder =>
        {
            var allowedOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>();
            corsBuilder.WithOrigins(allowedOrigins!)
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

/// Auth
builder.Services.AddIdentity<SiteRestUser, IdentityRole>()
    .AddEntityFrameworkStores<SiteDbContext>()
    .AddDefaultTokenProviders();


builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters.ValidAudience = builder.Configuration["Jwt:ValidAudience"];
    options.TokenValidationParameters.ValidIssuer = builder.Configuration["Jwt:ValidIssuer"];
    options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]));
});

builder.Services.AddAuthorization();

/// App
var app = builder.Build();

app.UseCors();

app.UseMiddleware<TokenValidationMiddleware>();



app.AddCountryAPI();
app.AddPlaceAPI();
app.AddCommentAPI();
app.AddAuthAPI();

/// Run app
app.UseAuthentication();
app.UseAuthorization();

using var scope = app.Services.CreateScope();

var dbContext = scope.ServiceProvider.GetRequiredService<SiteDbContext>();
dbContext.Database.Migrate();

var dbSeeder = scope.ServiceProvider.GetRequiredService<AuthDbSeeder>();

await dbSeeder.SeedAsync();

app.Run();
