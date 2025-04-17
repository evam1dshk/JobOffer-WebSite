using System;
using System.ComponentModel.DataAnnotations;
using JobListingSite.Data.Entities;
using JobListingSite.Data.Enums;

namespace JobListingSite.Data.Entities
{
    public class JobEditRequest
    {
        public int Id { get; set; }

        [Required]
        public int OfferId { get; set; }
        public Offer Offer { get; set; }

        [Required]
        public string RequestedChanges { get; set; }

        public string? AdditionalComments { get; set; }

        [Required]
        public EditPriority Priority { get; set; } = EditPriority.Normal;

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    }
}
