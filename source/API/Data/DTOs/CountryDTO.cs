using FluentValidation;
using Saitynai2.Auth.Model;
using Saitynai2.Data.Entities;

namespace Saitynai2.Data.DTOs
{
    /// <summary>
    /// Records
    /// </summary>
    public record CountryDto(int id, string Name, string Description, DateTime CreationDate, string? UserId = "", SiteRestUser? User = null);
    public record CreateCountryDto(string Name, string Description);
    public record UpdateCountryDto(string Description);





    /// <summary>
    /// Methods
    /// </summary>
    public class CountryDtoMethods
    {
        public static CountryDto MakeDto(Country country, bool adminUser)
        {
            if(adminUser) return new CountryDto(country.Id, country.Name, country.Description, country.CreationDate, country.UserId, country.User);
            else return new CountryDto(country.Id, country.Name, country.Description, country.CreationDate);
        }
    }


    /// <summary>
    /// Validators
    /// </summary>
    public class CreateCountryDtoValidator : AbstractValidator<CreateCountryDto>
    {
        public CreateCountryDtoValidator() 
        {
            RuleFor(dto => dto.Name).NotEmpty().NotNull().Length(2, 100);
            RuleFor(dto => dto.Description).NotEmpty().NotNull().Length(5, 300);
        }
    }

    public class UpdateCountryDtoValidator : AbstractValidator<UpdateCountryDto>
    {
        public UpdateCountryDtoValidator()
        {
            RuleFor(dto => dto.Description).NotEmpty().NotNull().Length(5, 300);
        }
    }
}
