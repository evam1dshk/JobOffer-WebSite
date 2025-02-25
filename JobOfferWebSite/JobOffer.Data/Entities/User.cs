using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobOffer.Data.Entities
{
    public class User
    {
        public int UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = null!;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        public string PasswordHash { get; set; } = null!;
        public bool IsCompany { get; set; } 
        public bool IsApproved { get; set; }

        [Required]
        public int RoleId { get; set; }
        public Role Role { get; set; } = null!;

        public Profile Profile { get; set; } = null!;
    }
}
