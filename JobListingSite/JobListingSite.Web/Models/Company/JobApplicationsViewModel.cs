using JobListingSite.Data.Entities;
using JobListingSite.Data.Enums;
using X.PagedList;

namespace JobListingSite.Web.Models.Company
{
    public class JobApplicationsViewModel
    {
        public int OfferId { get; set; }
        public string OfferTitle { get; set; }
        public List<ApplicationViewModel> Applications { get; set; } = new();
        public IPagedList<ApplicationViewModel> ApplicationsPaged { get; set; }
    }

    public class ApplicationViewModel
    {
        public int Id { get; set; }
        public string ApplicantName { get; set; }
        public string ApplicantEmail { get; set; }
        public DateTime AppliedOn { get; set; }
        public ApplicationStatus Status { get; set; }
        public string UserId { get; set; }
        public string ProfilePictureUrl { get; set; }
        public string ApplicantId { get; internal set; }
        public string? ResumeFilePath { get; set; }

        public string TimeAgo => GetTimeAgo(AppliedOn);

        public string? ProfileImageUrl { get; internal set; }

        private string GetTimeAgo(DateTime dateTime)
        {
            var span = DateTime.UtcNow - dateTime;

            if (span.TotalDays > 1)
                return $"{(int)span.TotalDays} days ago";
            if (span.TotalHours >= 1)
                return $"{(int)span.TotalHours} hours ago";
            if (span.TotalMinutes >= 1)
                return $"{(int)span.TotalMinutes} minutes ago";

            return "Just now";
        }

    }
}
