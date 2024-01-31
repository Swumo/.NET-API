using FluentValidation;
using Saitynai2.Auth.Model;
using Saitynai2.Data.Entities;

namespace Saitynai2.Data.DTOs
{
    /// <summary>
    /// Records
    /// </summary>
    public record CommentDto(int id, string Content, DateTime CreationDate, Place Place, string? UserId = "", SiteRestUser? User = null);
    public record CreateCommentDto(string Content);
    public record UpdateCommentDto(string Content);





    /// <summary>
    /// Methods
    /// </summary>
    public class CommentDtoMethods
    {
        public static CommentDto MakeDto(Comment comment, bool adminUser)
        {
            if(adminUser) return new CommentDto(comment.Id, comment.Content, comment.CreationDate, comment.Place, comment.UserId, comment.User);
            else return new CommentDto(comment.Id, comment.Content, comment.CreationDate, comment.Place, "", comment.User);
        }
    }



    /// <summary>
    /// Validators
    /// </summary>
    public class CreateCommentDtoValidator : AbstractValidator<CreateCommentDto>
    {
        public CreateCommentDtoValidator()
        {
            RuleFor(dto => dto.Content).NotEmpty().NotNull().Length(1, 300);
        }
    }

    public class UpdateCommentDtoValidator : AbstractValidator<UpdateCommentDto>
    {
        public UpdateCommentDtoValidator()
        {
            RuleFor(dto => dto.Content).NotEmpty().NotNull().Length(1, 300);
        }
    }
}
