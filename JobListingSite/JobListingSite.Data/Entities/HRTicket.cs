using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JobListingSite.Data.Enums;

namespace JobListingSite.Data.Entities
{
    public class HRTicket
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = null!;

        [Required]
        [MaxLength(1000)]
        public string Description { get; set; } = null!;

        [Required]
        public TicketStatus Status { get; set; } = TicketStatus.Open;

        [Required]
        public TicketPriority Priority { get; set; } = TicketPriority.Normal;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ResolvedAt { get; set; }

        [ForeignKey("User")]
        public string CreatedById { get; set; } = null!;
        public User? CreatedBy { get; set; } = null!;
        public string? AdminReply { get; set; }

        public DateTime? RepliedAt { get; set; }
        public string? HRReply { get; set; }
        public DateTime? HRRepliedAt { get; set; }
        public bool IsReadByAdmin { get; set; } = true;
        public bool IsReadByHR { get; set; } = true;

    }
}
