using JobListingSite.Web.Models.JobListing;

namespace JobListingSite.Web.Models.Home
{
    public class HomeViewModel
    {
        public string SearchTerm { get; set; }
        public List<OfferCardViewModel> FeaturedJobs { get; set; }
    }
}
