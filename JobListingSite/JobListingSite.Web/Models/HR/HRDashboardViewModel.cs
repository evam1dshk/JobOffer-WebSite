using JobListingSite.Data.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;
using X.PagedList;

namespace JobListingSite.Web.Models.HR
{
    public class HRDashboardViewModel
    {
        public int TotalJobs { get; set; }
        public int TotalApplications { get; set; }
        public int PendingApplications { get; set; }
        public int ApprovedApplications { get; set; }
        public int RejectedApplications { get; set; }

        public IPagedList<Offer> RecentOffers { get; set; }

        public string? SearchQuery { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public List<SelectListItem>? AllCategories { get; set; }

        public int? SelectedCategoryId { get; set; }
    }
}
