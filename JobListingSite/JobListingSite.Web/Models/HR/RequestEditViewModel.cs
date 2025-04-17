using JobListingSite.Data.Enums;
using System.ComponentModel.DataAnnotations;

namespace JobListingSite.Web.Models.HR
{
   public class RequestEditViewModel
    {
        public int OfferId { get; set; }
        public string JobTitle { get; set; }

        [Required(ErrorMessage = "Please describe the requested changes.")]
        public string RequestedChanges { get; set; }

        public string? AdditionalComments { get; set; }

        public EditPriority Priority { get; set; } = EditPriority.Normal;
    }
}