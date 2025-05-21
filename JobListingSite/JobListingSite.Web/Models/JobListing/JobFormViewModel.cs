using JobListingSite.Data.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace JobListingSite.Web.Models.JobListing
{
    public class JobFormViewModel
    {
        public int? OfferId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = null!;

        [Required]
        public string Description { get; set; } = null!;

        [Range(0, double.MaxValue)]
        public decimal Salary { get; set; }

        [Display(Name = "Category")]
        public int? CategoryId { get; set; }

        [Required(ErrorMessage = "Location is required.")]
        [Display(Name = "Location")]
        public string Location { get; set; }

        public IEnumerable<SelectListItem>? Categories { get; set; }
    }
}
