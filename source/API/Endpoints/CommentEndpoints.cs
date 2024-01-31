using Saitynai2.Data.DTOs;
using Saitynai2.Data;
using Microsoft.EntityFrameworkCore;
using O9d.AspNet.FluentValidation;
using Saitynai2.Data.Entities;
using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Saitynai2.Helpers;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Saitynai2.Auth.Model;

namespace Saitynai2.Endpoints
{
    public static class CommentEndpoints
    {
        private const string getAll = "GetComments";
        private const string getOne = "GetComment";
        private const string createOne = "CreateComment";
        private const string editOne = "EditComment";
        private const string deleteOne = "DeleteComment";

        public static void AddCommentAPI(this WebApplication app)
        {
            var commentGroup = app.MapGroup("/api/countries/{countryId}/places/{placeId}").WithValidationFilter();
            commentGroup.MapGet("comments", [Authorize(Roles = SiteRoles.SiteUser)] async (int countryId, int placeId, [AsParameters] SearchParameters searchParams, SiteDbContext dbContext, LinkGenerator linkGenerator, HttpContext httpContext, CancellationToken cancellationToken) =>
            {
                var queryable = (IQueryable<Comment>?)null;
                bool adminUser = false;
                if (httpContext.User.IsInRole(SiteRoles.Admin))
                {
                    adminUser = true;
                }
                if (adminUser)
                {
                    queryable = dbContext.Comments.Include(p => p.Place).Include(p => p.Place.User).Include(p => p.Place.Country).AsQueryable().OrderBy(place => place.CreationDate).Where(p => p.Place.Id == placeId && p.Place.Country.Id == countryId);
                }
                else
                {
                    queryable = dbContext.Comments.Include(u => u.User).AsQueryable().OrderBy(place => place.CreationDate).Where(p => p.Place.Id == placeId && p.Place.Country.Id == countryId);
                }
                var pagedList = await PagedList<Comment>.CreateAsync(queryable, searchParams.PageNumber!.Value, searchParams.PageSize!.Value);

                var prevPageLink = pagedList.HasPrevious ? linkGenerator.GetUriByName(httpContext, getAll,
                new { pageNumber = searchParams.PageNumber - 1, pageSize = searchParams.PageSize })
                : null;

                var nextPageLink = pagedList.HasNext ? linkGenerator.GetUriByName(httpContext, getAll,
                    new { pageNumber = searchParams.PageNumber + 1, pageSize = searchParams.PageSize })
                : null;

                var paginationMetadata = new PaginationMetadata(pagedList.TotalItems, pagedList.PageSize, pagedList.CurrentPage, pagedList.TotalPages, prevPageLink, nextPageLink);


                httpContext.Response.Headers.Add("Pagination", JsonSerializer.Serialize(paginationMetadata));

                return pagedList.Select(comment => CommentDtoMethods.MakeDto(comment, adminUser));

                // Place comments
                //var comments = (await dbContext.Comments.ToListAsync(cancellationToken)).Where(comment => comment.Place.Id == placeId);
                //(await dbContext.Places.Include(p => p.Country).ToListAsync(cancellationToken)).Where(place => place.Country.Id == countryId);

                //return comments.Where(comment => comment.Place.Id == placeId).Select(comment => CommentDtoMethods.MakeDto(comment));

                //All comments
                //var comments = await dbContext.Comments.Include("Place").ToListAsync(cancellationToken);
                //await dbContext.Places.Include("Country").ToListAsync(cancellationToken);
                //return comments.Select(comment => CommentDtoMethods.MakeCommentDto(comment));
            }).WithName(getAll);

            commentGroup.MapGet("comments/{commentId}", [Authorize(Roles = SiteRoles.SiteUser)] async (int countryId, int placeId, int commentId, HttpContext httpContext, LinkGenerator linkGenerator, SiteDbContext dbContext) =>
            {

                var country = (Country?)null;
                var place = (Place?)null;
                var comment = (Comment?)null;

                bool adminUser = false;
                if (httpContext.User.IsInRole(SiteRoles.Admin))
                {
                    adminUser = true;
                }
                if (adminUser)
                {
                    country = await dbContext.Countries.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == countryId);
                    place = await dbContext.Places.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == placeId && p.Country.Id == countryId);
                    comment = await dbContext.Comments.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == commentId && c.Place.Id == placeId && c.Place.Country.Id == countryId);
                }
                else
                {
                    country = await dbContext.Countries.FirstOrDefaultAsync(c => c.Id == countryId);
                    place = await dbContext.Places.FirstOrDefaultAsync(p => p.Id == placeId && p.Country.Id == countryId);
                    comment = await dbContext.Comments.FirstOrDefaultAsync(c => c.Id == commentId && c.Place.Id == placeId && c.Place.Country.Id == countryId);
                }
                if (country == null)
                    return Results.NotFound();
                if (place == null)
                    return Results.NotFound();
                if (comment == null)
                    return Results.NotFound();

                //var links = CreateLinks(country.Id, place.Id, comment.Id, httpContext, linkGenerator);
                var commentDto = CommentDtoMethods.MakeDto(comment, adminUser);

                //var resource = new ResourceDTO<CommentDto>(commentDto, links.ToArray());

                return Results.Ok(commentDto);
            }).WithName(getOne);

            commentGroup.MapPost("comments", [Authorize(Roles = SiteRoles.SiteUser)] async (int countryId, int placeId, HttpContext httpContext, LinkGenerator linkGenerator, [Validate] CreateCommentDto createCommentDto, SiteDbContext dbContext) =>
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
                    place = await dbContext.Places.Include("Country").Include("User").FirstOrDefaultAsync(p => p.Id == placeId);
                    country = await dbContext.Countries.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == countryId);
                }
                else
                {
                    place = await dbContext.Places.Include("Country").FirstOrDefaultAsync(p => p.Id == placeId);
                    country = await dbContext.Countries.FirstOrDefaultAsync(c => c.Id == countryId);
                }
                if (place == null)
                {
                    return Results.NotFound();
                }
                if (country == null)
                {
                    return Results.NotFound();
                }

                var comment = new Comment()
                {
                    Content = createCommentDto.Content,
                    CreationDate = DateTime.UtcNow,
                    Place = place,
                    UserId = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                };

                dbContext.Comments.Add(comment);

                await dbContext.SaveChangesAsync();

                //var links = CreateLinks(country.Id, place.Id, comment.Id, httpContext, linkGenerator);
                var commentDto = CommentDtoMethods.MakeDto(comment, adminUser);

                //var resource = new ResourceDTO<CommentDto>(commentDto, links.ToArray());

                return Results.Created($"/api/countries/{country.Id}/places/{place.Id}/comments/{comment.Id}", commentDto);
            }).WithName(createOne);

            commentGroup.MapPut("comments/{commentId}", [Authorize(Roles = SiteRoles.SiteUser)] async (int countryId, int placeId, int commentId, HttpContext httpContext, LinkGenerator linkGenerator,  SiteDbContext dbContext, [Validate] UpdateCommentDto dto) =>
            {
                var country = (Country?)null;
                var place = (Place?)null;
                var comment = (Comment?)null;

                bool adminUser = false;
                if (httpContext.User.IsInRole(SiteRoles.Admin))
                {
                    adminUser = true;
                }
                if (adminUser)
                {
                    country = await dbContext.Countries.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == countryId);
                    place = await dbContext.Places.Include(p => p.User).Include(p => p.Country).FirstOrDefaultAsync(p => p.Id == placeId && p.Country.Id == countryId);
                    comment = await dbContext.Comments.Include(c => c.User).Include(c => c.Place).Include(c => c.Place.Country).FirstOrDefaultAsync(c => c.Id == commentId && c.Place.Id == placeId);
                }
                else
                {
                    country = await dbContext.Countries.FirstOrDefaultAsync(c => c.Id == countryId);
                    place = await dbContext.Places.Include(p => p.Country).FirstOrDefaultAsync(p => p.Id == placeId && p.Country.Id == countryId);
                    comment = await dbContext.Comments.Include(c => c.Place).FirstOrDefaultAsync(c => c.Id == commentId && c.Place.Id == placeId);
                }
                if (country == null)
                    return Results.NotFound();
                if (place == null)
                    return Results.NotFound();
                if (comment == null)
                    return Results.NotFound();

                if (!httpContext.User.IsInRole(SiteRoles.Admin) && httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) != comment.UserId)
                {
                    return Results.Unauthorized();
                }

                comment.Content = dto.Content;

                dbContext.Update(comment);
                await dbContext.SaveChangesAsync();


                //var links = CreateLinks(country.Id, place.Id, comment.Id, httpContext, linkGenerator);
                var commentDto = CommentDtoMethods.MakeDto(comment, adminUser);

                //var resource = new ResourceDTO<CommentDto>(commentDto, links.ToArray());

                return Results.Ok(commentDto);
            }).WithName(editOne);

            commentGroup.MapDelete("comments/{commentId}", [Authorize(Roles = SiteRoles.SiteUser)] async (int commentId, HttpContext httpContext, SiteDbContext dbContext) =>
            {
                var comment = await dbContext.Comments.FirstOrDefaultAsync(c => c.Id == commentId);
                if (comment == null)
                    return Results.NotFound();

                if (!httpContext.User.IsInRole(SiteRoles.Admin) && httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) != comment.UserId)
                {
                    return Results.Unauthorized();
                }
                dbContext.Remove(comment);
                await dbContext.SaveChangesAsync();

                return Results.NoContent();
            }).WithName(deleteOne);
        }

        static IEnumerable<LinkDTO> CreateLinks(int countryId, int placeId, int commentId, HttpContext httpContext, LinkGenerator linkGenerator)
        {
            yield return new LinkDTO(linkGenerator.GetUriByName(httpContext, getOne, new { countryId, placeId, commentId }), "self", "GET");
            yield return new LinkDTO(linkGenerator.GetUriByName(httpContext, editOne, new { countryId, placeId, commentId }), "edit", "PUT");
            yield return new LinkDTO(linkGenerator.GetUriByName(httpContext, deleteOne, new { countryId, placeId, commentId }), "delete", "DELETE");
        }
    }
}
