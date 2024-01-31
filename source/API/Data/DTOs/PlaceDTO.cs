using FluentValidation;
using Saitynai2.Auth.Model;
using Saitynai2.Data.Entities;

namespace Saitynai2.Data.DTOs
{
    /// <summary>
    /// Records
    /// </summary>
    public record PlaceDto(int id, string Name, string Body, DateTime CreationDate, Country Country, string? UserId = "", SiteRestUser? User = null);
    public record CreatePlaceDto(string Name, string Body);
    public record UpdatePlaceDto(string Body);





    /// <summary>
    /// Methods
    /// </summary>
    public class PlaceDtoMethods
    {
        public static PlaceDto MakeDto(Place place, bool adminUser)
        {
            if(adminUser) return new PlaceDto(place.Id, place.Name, place.Body, place.CreationDate, place.Country, place.UserId, place.User);
            else return new PlaceDto(place.Id, place.Name, place.Body, place.CreationDate, place.Country);   
        }
    }



    /// <summary>
    /// Validators
    /// </summary>
    public class CreatePlaceDtoValidator : AbstractValidator<CreatePlaceDto>
    {
        public CreatePlaceDtoValidator()
        {
            RuleFor(dto => dto.Name).NotEmpty().NotNull().Length(2, 100);
            RuleFor(dto => dto.Body).NotEmpty().NotNull().Length(5, 300);
        }
    }

    public class UpdatePlaceDtoValidator : AbstractValidator<UpdatePlaceDto>
    {
        public UpdatePlaceDtoValidator()
        {
            RuleFor(dto => dto.Body).NotEmpty().NotNull().Length(5, 300);
        }
    }
}
