using JobListingSite.Data.Enums;

namespace JobListingSite.Web.Models.Company
{
    public class JobApplicationsViewModel
    {
        public int OfferId { get; set; }
        public string OfferTitle { get; set; }
        public List<ApplicationViewModel> Applications { get; set; } = new();
    }

    public class ApplicationViewModel
    {
        public int Id { get; set; } // JobApplication Id
        public string ApplicantName { get; set; }
        public string ApplicantEmail { get; set; }
        public DateTime AppliedOn { get; set; }
        public ApplicationStatus Status { get; set; }
    }
}
