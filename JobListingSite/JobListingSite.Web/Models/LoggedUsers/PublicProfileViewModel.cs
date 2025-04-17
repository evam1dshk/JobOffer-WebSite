namespace JobListingSite.Web.Models.LoggedUsers
{
    public class PublicProfileViewModel
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string? Bio { get; set; }
        public string? Phone { get; set; }
        public string? Location { get; set; }
        public string? LinkedInUrl { get; set; }
        public string? PortfolioUrl { get; set; }
        public string? ResumeFilePath { get; set; }
        public string? ProfileImageUrl { get; set; }
    }
}
