using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace JobListingSite.Data.Entities
{
    public class CompanyProfile
    {
        public int Id { get; set; }

        public string UserId { get; set; } = null!;

        [ValidateNever]

        public User? User { get; set; }

        [Required]
        [MaxLength(100)]
        public string CompanyName { get; set; } = null!;

        [Url]
        public string? CompanyWebsite { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public string? LogoUrl { get; set; }
        public string? BannerImageUrl { get; set; }

        public string? Location { get; set; }
        public int? FoundedYear { get; set; }
        public int? NumberOfEmployees { get; set; }

        [MaxLength(100)]
        public string? Industry { get; set; }

        [Url]
        public string? LinkedIn { get; set; }

        [Url]
        public string? Twitter { get; set; }

        [EmailAddress]
        [MaxLength(100)]
        public string? ContactEmail { get; set; }

        [Phone]
        public string? Phone { get; set; }

        public string? Culture { get; set; }

        public bool IsVerified { get; set; } = false;

        public ICollection<Offer> JobOffers { get; set; } = new List<Offer>();
    }

}
