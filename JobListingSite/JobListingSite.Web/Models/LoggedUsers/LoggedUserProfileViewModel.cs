using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace JobListingSite.Web.Models.LoggedUsers
{
    public class LoggedUserProfileViewModel
    {
        public string Name { get; set; }

        public string? Bio { get; set; }
        public string? Phone { get; set; }
        public string? Location { get; set; }
        public string? LinkedInUrl { get; set; }
        public string? PortfolioUrl { get; set; }
        public string? ProfileImageUrl { get; set; }
        public string? SelectedAvatar { get; set; }

        public IFormFile? Resume { get; set; }
        public string? ResumeFilePath { get; set; }
        public IFormFile? ProfileImage { get; set; }

    }

}
