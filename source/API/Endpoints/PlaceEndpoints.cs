using O9d.AspNet.FluentValidation;
using Saitynai2.Data.DTOs;
using Saitynai2.Data.Entities;
using Saitynai2.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Saitynai2.Helpers;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Saitynai2.Auth.Model;

namespace Saitynai2.Endpoints
{
    public static class PlaceEndpoints
    {
        private const string getAll = "GetPlaces";
        private const string getOne = "GetPlace";
        private const string createOne = "CreatePlace";
        private const string editOne = "EditPlace";
        private const string deleteOne = "DeletePlace";

        public static void AddPlaceAPI(this WebApplication app)
        {
            var placeGroup = app.MapGroup("/api/countries/{countryId}").WithValidationFilter();
            placeGroup.MapGet("places", [Authorize(Roles = SiteRoles.SiteUser)] async (int countryId, [AsParameters] SearchParameters searchParams, SiteDbContext dbContext, LinkGenerator linkGenerator, HttpContext httpContext, CancellationToken cancellationToken) =>
            {
                bool adminUser = false;
                if (httpContext.User.IsInRole(SiteRoles.Admin))
                {
                    adminUser = true;
                }

                var queryable = (IQueryable<Place>?)null;

                if (adminUser)
                {
                    queryable = dbContext.Places.Include(p => p.User).Include(p => p.Country).Include(p => p.Country.User).AsQueryable().OrderBy(place => place.CreationDate).Where(p => p.Country.Id == countryId);
                }
                else
                {
                    queryable = dbContext.Places.Include(p => p.Country).AsQueryable().OrderBy(place => place.CreationDate).Where(p => p.Country.Id == countryId);
                }

                var pagedList = await PagedList<Place>.CreateAsync(queryable, searchParams.PageNumber!.Value, searchParams.PageSize!.Value);

                var prevPageLink = pagedList.HasPrevious ? linkGenerator.GetUriByName(httpContext, getAll,
                new { pageNumber = searchParams.PageNumber - 1, pageSize = searchParams.PageSize })
                : null;

                var nextPageLink = pagedList.HasNext ? linkGenerator.GetUriByName(httpContext, getAll,
                    new { pageNumber = searchParams.PageNumber + 1, pageSize = searchParams.PageSize })
                : null;

                var paginationMetadata = new PaginationMetadata(pagedList.TotalItems, pagedList.PageSize, pagedList.CurrentPage, pagedList.TotalPages, prevPageLink, nextPageLink);

                httpContext.Response.Headers.Add("Pagination", JsonSerializer.Serialize(paginationMetadata));

                return pagedList.Select(place => PlaceDtoMethods.MakeDto(place, adminUser));

                // Country place
                //return (await dbContext.Places.Include(p => p.Country).ToListAsync(cancellationToken)).Where(place => place.Country.Id == countryId).Select(place => PlaceDtoMethods.MakePlaceDto(place));

                //All places
                //return (await dbContext.Places.Include("Country").ToListAsync(cancellationToken)).Select(place => PlaceDtoMethods.MakePlaceDto(place));
            }).WithName(getAll);

            placeGroup.MapGet("places/{placeId}", [Authorize(Roles = SiteRoles.SiteUser)] async (int countryId, int placeId, HttpContext httpContext, LinkGenerator linkGenerator, SiteDbContext dbContext) =>
            {
                var country = (Country?)null;
                var place = (Place?)null;

                bool adminUser = false;
                if (httpContext.User.IsInRole(SiteRoles.Admin))
                {
                    adminUser = true;
                }
                if (adminUser)
                {
                    country = await dbContext.Countries.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == countryId);
                    place = await dbContext.Places.Include(p => p.Country).Include(p => p.Country.User).FirstOrDefaultAsync(p => p.Id == placeId && p.Country.Id == countryId);
                }
                else
                {
                    country = await dbContext.Countries.FirstOrDefaultAsync(c => c.Id == countryId);
                    place = await dbContext.Places.FirstOrDefaultAsync(p => p.Id == placeId && p.Country.Id == countryId);
                }
                if (country == null)
                    return Results.NotFound();
                if (place == null)
                    return Results.NotFound();

                //var links = CreateLinks(country.Id, place.Id, httpContext, linkGenerator);
                var placeDto = PlaceDtoMethods.MakeDto(place, adminUser);

                //var resource = new ResourceDTO<PlaceDto>(placeDto, links.ToArray());

                return Results.Ok(placeDto);
            }).WithName(getOne);

            placeGroup.MapPost("places", [Authorize(Roles = SiteRoles.SiteUser)] async (int countryId, HttpContext httpContext, LinkGenerator linkGenerator, [Validate] CreatePlaceDto createPlaceDto, SiteDbContext dbContext) =>
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
                {
                    return Results.NotFound();
                }

                var exists = await dbContext.Places.FirstOrDefaultAsync(p => p.Name == createPlaceDto.Name);
                if (exists != null)
                {
                    var error = new { error = "Place already exists" };
                    return Results.Conflict(error);
                }

                var place = new Place()
                {
                    Name = createPlaceDto.Name,
                    Body = createPlaceDto.Body,
                    CreationDate = DateTime.UtcNow,
                    Country = country,
                    UserId = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                };

                dbContext.Places.Add(place);

                await dbContext.SaveChangesAsync();

                //var links = CreateLinks(country.Id, place.Id, httpContext, linkGenerator);
                var placeDto = PlaceDtoMethods.MakeDto(place, adminUser);

                //var resource = new ResourceDTO<PlaceDto>(placeDto, links.ToArray());

                return Results.Created($"/api/countries/{country.Id}/places/{place.Id}", placeDto);
            }).WithName(createOne);

            placeGroup.MapPut("places/{placeId}", [Authorize(Roles = SiteRoles.SiteUser)] async (int countryId, int placeId, HttpContext httpContext, LinkGenerator linkGenerator, SiteDbContext dbContext, [Validate] UpdatePlaceDto dto) =>
            {

                var country = (Country?)null;
                var place = (Place?)null;

                bool adminUser = false;
                if (httpContext.User.IsInRole(SiteRoles.Admin))
                {
                    adminUser = true;
                }
                if (adminUser)
                {
                    country = await dbContext.Countries.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == countryId);
                    place = await dbContext.Places.Include(p => p.Country).Include(p => p.Country.User).FirstOrDefaultAsync(p => p.Id == placeId && p.Country.Id == countryId);
                }
                else
                {
                    country = await dbContext.Countries.FirstOrDefaultAsync(c => c.Id == countryId);
                    place = await dbContext.Places.FirstOrDefaultAsync(p => p.Id == placeId && p.Country.Id == countryId);
                }
                if (country == null)
                    return Results.NotFound();
                if (place == null)
                    return Results.NotFound();

                if (!httpContext.User.IsInRole(SiteRoles.Admin) && httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) != place.UserId)
                {
                    return Results.Unauthorized();
                }

                place.Body = dto.Body;

                dbContext.Update(place);
                await dbContext.SaveChangesAsync();

                //var links = CreateLinks(country.Id, place.Id, httpContext, linkGenerator);
                var placeDto = PlaceDtoMethods.MakeDto(place, adminUser);

                //var resource = new ResourceDTO<PlaceDto>(placeDto, links.ToArray());

                return Results.Ok(placeDto);
            }).WithName(editOne);

            placeGroup.MapDelete("places/{placeId}", [Authorize(Roles = SiteRoles.SiteUser)] async (int placeId, SiteDbContext dbContext, HttpContext httpContext) =>
            {
                var place = await dbContext.Places.FirstOrDefaultAsync(c => c.Id == placeId);
                if (place == null)
                    return Results.NotFound();

                if (!httpContext.User.IsInRole(SiteRoles.Admin) && httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) != place.UserId)
                {
                    return Results.Unauthorized();
                }

                dbContext.Remove(place);
                await dbContext.SaveChangesAsync();

                return Results.NoContent();
            }).WithName(deleteOne);
        }


        static IEnumerable<LinkDTO> CreateLinks(int countryId, int placeId, HttpContext httpContext, LinkGenerator linkGenerator)
        {
            yield return new LinkDTO(linkGenerator.GetUriByName(httpContext, getOne, new { countryId, placeId }), "self", "GET");
            yield return new LinkDTO(linkGenerator.GetUriByName(httpContext, editOne, new { countryId, placeId }), "edit", "PUT");
            yield return new LinkDTO(linkGenerator.GetUriByName(httpContext, deleteOne, new { countryId, placeId }), "delete", "DELETE");
        }
    }
}
