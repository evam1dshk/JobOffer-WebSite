using Microsoft.AspNetCore.Mvc.Rendering;

namespace JobListingSite.Web.Models.JobListing
{
     public class BrowseViewModel
    {
        public List<OfferCardViewModel> Offers { get; set; }
        public string? SearchTerm { get; set; }
        public int? CategoryId { get; set; }
        public List<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
        public string CompanyUserId { get; set; } = null!;
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}
