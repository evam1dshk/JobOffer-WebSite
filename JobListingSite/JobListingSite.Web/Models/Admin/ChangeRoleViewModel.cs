using System.ComponentModel.DataAnnotations;

namespace JobListingSite.Web.Models.Admin
{
    public class ChangeRoleViewModel
    {
        public string UserId { get; set; }

        public string CurrentRole { get; set; }

        [Required]
        public string NewRole { get; set; }

        public List<string> AvailableRoles { get; set; }
        public string UserName { get; set; }
    }
}
