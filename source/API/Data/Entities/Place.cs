using Saitynai2.Auth.Model;
using System.ComponentModel.DataAnnotations;

namespace Saitynai2.Data.Entities
{
    public class Place
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Body { get; set; }
        public required DateTime CreationDate { get; set; }
        public required Country Country { get; set; }


        [Required]
        public required string UserId { get; set; }
        public SiteRestUser User { get; set; }


        public override string ToString()
        {
            if(this.Country == null)
                return this.Id.ToString() + " " + this.Name.ToString() + " " + this.Body + " " + this.CreationDate.ToString() + " " + "null";
            else
                return this.Id.ToString() + " " + this.Name.ToString() + " " + this.Body + " " + this.CreationDate.ToString() + " " +$"{this.Country}" ;
        }
    }
}
