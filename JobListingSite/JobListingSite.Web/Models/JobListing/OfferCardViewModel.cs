namespace JobListingSite.Web.Models.JobListing
{
    public class OfferCardViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string DescriptionSnippet { get; set; } = null!;
        public string CompanyName { get; set; } = null!;
        public string CategoryName { get; set; } = null!;
        public string CompanyUserId { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public int ApplicantsCount { get; set; }
        public string? LogoUrl { get; set; }
    }

}
