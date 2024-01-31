using Saitynai2.Auth.Model;
using System.ComponentModel.DataAnnotations;

namespace Saitynai2.Data.Entities
{
    public class Country 
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required DateTime CreationDate { get; set; }

        [Required]
        public required string UserId { get; set; }
        public SiteRestUser User { get; set; }


        public override string ToString()
        {
            return this.Id.ToString() + " " + this.Name.ToString() + " " + this.Description + " " + this.CreationDate.ToString();
        }
    }

}
