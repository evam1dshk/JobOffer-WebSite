using System.ComponentModel.DataAnnotations;

namespace JobListingSite.Web.Models.Company
{
    public class CompanyProfileFormViewModel
    {
        [Required]
        [Display(Name = "Company Name")]
        public string CompanyName { get; set; } = null!;

        [Display(Name = "Description")]
        public string? Description { get; set; }

        public string? Industry { get; set; }

        [EmailAddress]
        [Display(Name = "Contact Email")]
        public string? ContactEmail { get; set; }

        [Phone]
        [Display(Name = "Phone")]
        public string? Phone { get; set; }

        [Url]
        [Display(Name = "Company Website")]
        public string? CompanyWebsite { get; set; }

        [Display(Name = "FoundedDate")]
        [DataType(DataType.Date)]
        public DateTime? FoundedDate { get; set; }
        [Display(Name = "Company Location")]
        [StringLength(100, ErrorMessage = "Location must be under 100 characters.")]
        public string? Location { get; set; }
        public string? LinkedIn { get; set; }
        public string? Twitter { get; set; }
        public int? NumberOfEmployees { get; set; }
        public IFormFile? Logo { get; set; }
        public string? LogoUrl { get; set; }
    }
}
