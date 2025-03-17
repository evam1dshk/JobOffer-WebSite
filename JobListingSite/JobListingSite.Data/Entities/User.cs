using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobListingSite.Data.Entities
{
    public class User : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = null!;
        public bool IsCompany { get; set; }
        public bool IsApproved { get; set; }
        public Profile? Profile { get; set; }
        public ICollection<Offer> Offers { get; set; } = new List<Offer>(); // Companies post job offers
        public ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();
    }
}
