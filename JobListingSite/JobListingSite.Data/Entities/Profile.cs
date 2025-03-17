using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobListingSite.Data.Entities
{
    public class Profile
    {
        public int ProfileId { get; set; }

        [MaxLength(500)]
        public string Bio { get; set; } = null!;

        [Url]
        public string? LinkedInUrl { get; set; }
        public string? UserId { get; set; }
        public User User { get; set; } = null!;
    }
}
