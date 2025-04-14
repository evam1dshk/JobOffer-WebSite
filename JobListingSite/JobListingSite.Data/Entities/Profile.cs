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
        public string? Bio { get; set; }
        public string? Phone { get; set; }
        public string? Location { get; set; }
        public string? LinkedInUrl { get; set; }
        public string? PortfolioUrl { get; set; }
        public string? ResumeFilePath { get; set; }
        public string? ProfileImageUrl { get; set; }
        public string? SelectedAvatar { get; set; }

        // Relationships
        public string UserId { get; set; }
        public User User { get; set; }
    }
}
