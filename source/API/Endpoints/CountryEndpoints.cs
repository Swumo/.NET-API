using O9d.AspNet.FluentValidation;
using Saitynai2.Data.DTOs;
using Saitynai2.Data.Entities;
using Saitynai2.Data;
using Microsoft.EntityFrameworkCore;
using Saitynai2.Helpers;
using System.Text.Json;
using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.AspNetCore.Authorization;
using Saitynai2.Auth.Model;
using Saitynai2.Auth;

namespace Saitynai2.Endpoints
{
    public static class CountryEndpoints
    {
        private const string getAll = "GetCountries";
        private const string getOne = "GetCountry";
        private const string createOne = "CreateCountry";
        private const string editOne = "EditCountry";
        private const string deleteOne = "DeleteCountry";

        public static void AddCountryAPI(this WebApplication app)
        {
            var countryGroup = app.MapGroup("/api").WithValidationFilter();
            countryGroup.MapGet("countries", [Authorize(Roles = SiteRoles.SiteUser)] async ([AsParameters] SearchParameters searchParams, SiteDbContext dbContext, LinkGenerator linkGenerator, HttpContext httpContext, CancellationToken cancellationToken) =>
            {
                var queryable = (IQueryable<Country>?)null;

                bool adminUser = false;
                if (httpContext.User.IsInRole(SiteRoles.Admin))
                {
                    adminUser = true;
                }
                if (adminUser)
                {
                    queryable = dbContext.Countries.Include(c => c.User).AsQueryable().OrderBy(country => country.CreationDate);
                }
                else
                {
                    queryable = dbContext.Countries.AsQueryable().OrderBy(country => country.CreationDate);
                }
                var pagedList = await PagedList<Country>.CreateAsync(queryable, searchParams.PageNumber!.Value, searchParams.PageSize!.Value);

                var prevPageLink = pagedList.HasPrevious ? linkGenerator.GetUriByName(httpContext, getAll,
                    new { pageNumber = searchParams.PageNumber - 1, pageSize = searchParams.PageSize })
                : null;

                var nextPageLink = pagedList.HasNext ? linkGenerator.GetUriByName(httpContext, getAll,
                    new { pageNumber = searchParams.PageNumber + 1, pageSize = searchParams.PageSize })
                : null;

                var paginationMetadata = new PaginationMetadata(pagedList.TotalItems, pagedList.PageSize, pagedList.CurrentPage, pagedList.TotalPages, prevPageLink, nextPageLink);

                httpContext.Response.Headers.Add("Pagination", JsonSerializer.Serialize(paginationMetadata));

                return pagedList.Select(country => CountryDtoMethods.MakeDto(country, adminUser));

            }).WithName(getAll);

            countryGroup.MapGet("countries/{countryId}", [Authorize(Roles = SiteRoles.SiteUser)] async (int countryId, HttpContext httpContext, LinkGenerator linkGenerator, SiteDbContext dbContext) =>
            {
                var country = (Country?)null;
                bool adminUser = false;
                if (httpContext.User.IsInRole(SiteRoles.Admin))
                {
                    adminUser = true;
                }
                if (adminUser)
                {
                    country = await dbContext.Countries.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == countryId);
                }
                else
                {
                    country = await dbContext.Countries.FirstOrDefaultAsync(c => c.Id == countryId);
                }
                if (country == null)
                    return Results.NotFound();

                //var links = CreateLinks(countryId, httpContext, linkGenerator);
                var countryDto = CountryDtoMethods.MakeDto(country, adminUser);

                //var resource = new ResourceDTO<CountryDto>(countryDto, links.ToArray());


                return Results.Ok(countryDto);
            }).WithName(getOne);

            countryGroup.MapPost("countries", [Authorize(Roles = SiteRoles.SiteUser)] async ([Validate] CreateCountryDto createCountryDto, HttpContext httpContext, TokenService tokenService, LinkGenerator linkGenerator, SiteDbContext dbContext) =>
            {
                var exists = await dbContext.Countries.Include(c => c.User).FirstOrDefaultAsync(c => c.Name == createCountryDto.Name);
                if (exists != null)
                {
                    var error = new { error = "Country already exists" };
                    return Results.Conflict(error);
                }
                var country = new Country()
                {
                    Name = createCountryDto.Name,
                    Description = createCountryDto.Description,
                    CreationDate = DateTime.UtcNow,
                    UserId = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                };

                bool adminUser = false;
                if (httpContext.User.IsInRole(SiteRoles.Admin))
                {
                    adminUser = true;
                }

                dbContext.Countries.Add(country);

                await dbContext.SaveChangesAsync();

                //var links = CreateLinks(country.Id, httpContext, linkGenerator);
                var countryDto = CountryDtoMethods.MakeDto(country, adminUser);

                //var resource = new ResourceDTO<CountryDto>(countryDto, links.ToArray());

                return Results.Created($"/api/countries/{country.Id}", countryDto);
            }).WithName(createOne);

            countryGroup.MapPut("countries/{countryId}", [Authorize(Roles = SiteRoles.SiteUser)] async (int countryId, HttpContext httpContext, TokenService tokenService, LinkGenerator linkGenerator, SiteDbContext dbContext, [Validate] UpdateCountryDto dto) =>
            {
                var country = (Country?)null;
                bool adminUser = false;
                if (httpContext.User.IsInRole(SiteRoles.Admin))
                {
                    adminUser = true;
                }
                if (adminUser)
                {
                    country = await dbContext.Countries.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == countryId);
                }
                else
                {
                    country = await dbContext.Countries.FirstOrDefaultAsync(c => c.Id == countryId);
                }
                if (country == null)
                    return Results.NotFound();

                if (!httpContext.User.IsInRole(SiteRoles.Admin) && httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) != country.UserId)
                {
                    return Results.Unauthorized();
                }

                country.Description = dto.Description;

                dbContext.Update(country);
                await dbContext.SaveChangesAsync();

                //var links = CreateLinks(country.Id, httpContext, linkGenerator);
                var countryDto = CountryDtoMethods.MakeDto(country, adminUser);

                //var resource = new ResourceDTO<CountryDto>(countryDto, links.ToArray());

                return Results.Ok(countryDto);
            }).WithName(editOne);

            countryGroup.MapDelete("countries/{countryId}", [Authorize(Roles = SiteRoles.SiteUser)] async (int countryId, SiteDbContext dbContext, HttpContext httpContext, TokenService tokenService) =>
            {
                var country = await dbContext.Countries.FirstOrDefaultAsync(c => c.Id == countryId);
                if (country == null)
                    return Results.NotFound();

                if (!httpContext.User.IsInRole(SiteRoles.Admin) && httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) != country.UserId)
                {
                    return Results.Unauthorized();
                }

                dbContext.Remove(country);
                await dbContext.SaveChangesAsync();

                return Results.NoContent();
            }).WithName(deleteOne);
        }

        static IEnumerable<LinkDTO> CreateLinks(int countryId, HttpContext httpContext, LinkGenerator linkGenerator)
        {
            yield return new LinkDTO(linkGenerator.GetUriByName(httpContext, getOne, new { countryId }), "self", "GET");
            yield return new LinkDTO(linkGenerator.GetUriByName(httpContext, editOne, new { countryId }), "edit", "PUT");
            yield return new LinkDTO(linkGenerator.GetUriByName(httpContext, deleteOne, new { countryId }), "delete", "DELETE");
        }
    }
}
