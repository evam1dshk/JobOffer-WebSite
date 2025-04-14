using JobListingSite.Data.Enums;
using System;

namespace JobListingSite.Web.Models.LoggedUsers
{
    public class MyApplicationsViewModel
    {
        public int ApplicationId { get; set; }
        public string JobTitle { get; set; } = null!;
        public string CompanyName { get; set; } = null!;
        public DateTime AppliedOn { get; set; }
        public ApplicationStatus Status { get; set; }
        public string? ProfileImageUrl { get; set; }
        public string? SelectedAvatar { get; set; }
    }
}
