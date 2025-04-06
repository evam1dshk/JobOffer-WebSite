using Microsoft.AspNetCore.Mvc.Rendering;

namespace JobListingSite.Web.Models.JobListing
{
     public class BrowseViewModel
    {
        public string? SearchTerm { get; set; }
        public int? CategoryId { get; set; }
        public List<SelectListItem> Categories { get; set; } = new();
        public List<OfferCardViewModel> Offers { get; set; } = new();
    }
}
