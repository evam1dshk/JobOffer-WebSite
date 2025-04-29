using JobListingSite.Data.Entities;
using JobListingSite.Web.Data;
using JobListingSite.Web.Models;
using JobListingSite.Web.Models.Home;
using JobListingSite.Web.Models.JobListing;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;


namespace JobListingSite.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly JobListingDbContext _context;

        public HomeController(UserManager<User> userManager, JobListingDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index(string searchTerm = null)
        {
            var offers = await _context.Offers
                .Include(o => o.Company)
                .ThenInclude(c => c.CompanyProfile)
                .Include(o => o.Category)
                .OrderBy(r => Guid.NewGuid())
                .Take(3)
                .ToListAsync();

            var featuredJobs = offers.Select(o => new OfferCardViewModel
            {
                Id = o.OfferId,
                Title = o.Title,
                CompanyName = o.Company != null ? o.Company.Name : "Unknown Company",
                Location = "Remote",
                CategoryName = o.Category != null ? o.Category.Name : "Uncategorized",
                CreatedAt = o.CreatedAt,
                ApplicantsCount = o.JobApplications.Count,
                DescriptionSnippet = o.Description.Length > 100 ? o.Description.Substring(0, 100) + "..." : o.Description,
                CompanyUserId = o.CompanyId ?? "",
                LogoUrl = o.Company != null ? o.Company.CompanyProfile?.LogoUrl : null
            }).ToList();

            var model = new HomeViewModel
            {
                SearchTerm = searchTerm,
                FeaturedJobs = featuredJobs
            };

            return View(model);
        }



        public IActionResult About()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
