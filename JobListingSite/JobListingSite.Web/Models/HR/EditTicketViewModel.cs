using JobListingSite.Data.Enums;
using System.ComponentModel.DataAnnotations;

namespace JobListingSite.Web.Models.HR
{
    public class EditTicketViewModel
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Title { get; set; } = "";

        [Required, MaxLength(1000)]
        public string Description { get; set; } = "";

        [Required]
        public TicketPriority Priority { get; set; }
    }
}
