using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobListingSite.Web.Data;
using JobListingSite.Data.Entities;

namespace JobListingSite.Web.Controllers
{
    [Authorize]
    public class JobController : Controller
    {
        private readonly JobListingDbContext _context;
        private readonly UserManager<User> _userManager;

        public JobController(JobListingDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Existing actions: Index, Details...

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(int offerId)
        {
            var user = await _userManager.GetUserAsync(User);

            // Check if the job offer exists
            var offer = await _context.Offers.FindAsync(offerId);
            if (offer == null)
            {
                TempData["ErrorMessage"] = "Job offer not found.";
                return RedirectToAction("Index");
            }

            // Check if the user already applied
            var alreadyApplied = await _context.JobApplications
                .AnyAsync(a => a.OfferId == offerId && a.UserId == user.Id);

            if (alreadyApplied)
            {
                TempData["WarningMessage"] = "You have already applied for this job.";
                return RedirectToAction("Details", new { id = offerId });
            }

            var application = new JobApplication
            {
                OfferId = offerId,
                UserId = user.Id,
                AppliedOn = DateTime.UtcNow
            };

            _context.JobApplications.Add(application);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your application has been submitted!";
            return RedirectToAction("Details", new { id = offerId });
        }
    }
}
