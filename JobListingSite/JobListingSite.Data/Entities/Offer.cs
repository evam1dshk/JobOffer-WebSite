using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobListingSite.Data.Entities
{
    public class Offer
    {
        public int OfferId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public decimal Salary { get; set; }

        public int? CategoryId { get; set; }
        public Category? Category { get; set; }

        public string? CompanyId { get; set; }
        public User? Company { get; set; }
        public ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();
    }
}
