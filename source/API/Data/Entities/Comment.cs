using Saitynai2.Auth.Model;
using System.ComponentModel.DataAnnotations;

namespace Saitynai2.Data.Entities
{
    public class Comment
    {
        public int Id { get; set; }
        public required string Content { get; set; }
        public required DateTime CreationDate { get; set; }

        public required Place Place { get; set; }

        [Required]
        public required string UserId { get; set; }
        public SiteRestUser User { get; set; }


        public override string ToString()
        {
            return this.Id.ToString() + " " + this.Content.ToString() + " " + this.CreationDate.ToString() + " " + this.Place.ToString();
        }
    }
}
