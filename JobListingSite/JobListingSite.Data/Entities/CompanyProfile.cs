using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobListingSite.Data.Entities
{
    public class CompanyProfile
    {
        public int Id { get; set; }

        public string UserId { get; set; } = null!;
        public User User { get; set; } = null!;

        public string CompanyName { get; set; } = null!;
        public string? CompanyWebsite { get; set; }
        public string? Description { get; set; }

        public string? LogoUrl { get; set; }
        public string? BannerImageUrl { get; set; }

        public string? Location { get; set; }
        public int? FoundedYear { get; set; }
        public int? NumberOfEmployees { get; set; }
        public string? Industry { get; set; }

        public string? LinkedIn { get; set; }
        public string? Twitter { get; set; }

        public string? ContactEmail { get; set; }
        public string? Phone { get; set; }

        public string? Culture { get; set; }

        public bool IsVerified { get; set; } = false;

        public ICollection<Offer> JobOffers { get; set; } = new List<Offer>();
    }

}
