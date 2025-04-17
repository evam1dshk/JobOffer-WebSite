using JobListingSite.Data.Entities;
using JobListingSite.Data.Enums;
using JobListingSite.Web.Data;
using JobListingSite.Web.Models.Company;
using JobListingSite.Web.Models.HR;
using JobListingSite.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Data;
using X.PagedList;
using X.PagedList.Extensions;

namespace JobListingSite.Web.Controllers
{
    [Authorize(Roles = "HR, Admin")]
    public class HRController : Controller
    {
        private readonly JobListingDbContext _context;

        public HRController(JobListingDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Dashboard(string search, int page = 1)
        {
            var pageSize = 10;

            var offersQuery = _context.Offers
                .Include(o => o.Category)
                .Include(o => o.JobApplications)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                offersQuery = offersQuery.Where(o =>
                    o.Title.Contains(search) ||
                    o.Category.Name.Contains(search));
            }

            var offersPaged = offersQuery
                .OrderByDescending(o => o.CreatedAt)
                .ToPagedList(page, pageSize);

            var dashboardVm = new HRDashboardViewModel
            {
                TotalJobs = await _context.Offers.CountAsync(),
                TotalApplications = await _context.JobApplications.CountAsync(),
                PendingApplications = await _context.JobApplications.CountAsync(a => a.Status == ApplicationStatus.Pending),
                RejectedApplications = await _context.JobApplications.CountAsync(a => a.Status == ApplicationStatus.Rejected),
                RecentOffers = offersPaged,
                SearchQuery = search,
                CurrentPage = offersPaged.PageNumber,
                TotalPages = offersPaged.PageCount
            };

            return View(dashboardVm);
        }


        public async Task<IActionResult> BrowseOffers(string search, int page = 1)
        {
            var pageSize = 10;
            var query = _context.Offers
                .Include(o => o.Category)
                .Include(o => o.JobApplications)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(o => o.Title.Contains(search) || o.Category.Name.Contains(search));
            }

            var offers = query.OrderByDescending(o => o.OfferId).ToPagedList(page, 5);
            ViewBag.SearchQuery = search;
            return View("BrowseOffers", offers);
        }

        public async Task<IActionResult> ViewApplications(int offerId)
        {
            var offer = await _context.Offers.Include(o => o.Category).FirstOrDefaultAsync(o => o.OfferId == offerId);
            if (offer == null) return NotFound();

            var applications = await _context.JobApplications
                .Include(a => a.User)
                    .ThenInclude(u => u.Profile)
                .Where(a => a.OfferId == offerId)
                .ToListAsync();

            var viewModel = new JobApplicationsViewModel
            {
                OfferId = offer.OfferId,
                OfferTitle = offer.Title,
                Applications = applications.Select(a => new ApplicationViewModel
                {
                    Id = a.Id,
                    ApplicantName = a.User.Name,
                    ApplicantEmail = a.User.Email,
                    Status = a.Status,
                    AppliedOn = a.AppliedOn,
                    ProfileImageUrl = a.User.Profile?.ProfileImageUrl ?? a.User.Profile?.SelectedAvatar,
                    ResumeFilePath = a.User.Profile?.ResumeFilePath,
                    ApplicantId = a.UserId
                }).ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveApplication(int applicationId)
        {
            var application = await _context.JobApplications.FindAsync(applicationId);
            if (application == null) return NotFound();

            application.Status = ApplicationStatus.Approved;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Application approved.";

            return RedirectToAction("ViewApplications", new { offerId = application.OfferId });
        }

        [HttpPost]
        public async Task<IActionResult> RejectApplication(int applicationId)
        {
            var application = await _context.JobApplications.FindAsync(applicationId);
            if (application == null) return NotFound();

            application.Status = ApplicationStatus.Rejected;
            await _context.SaveChangesAsync();
            TempData["Warning"] = "Application rejected.";

            return RedirectToAction("ViewApplications", new { offerId = application.OfferId });
        }
        public async Task<IActionResult> EditJob(int id)
        {
            var job = await _context.Offers.FindAsync(id);
            if (job == null) return NotFound();

            var categories = await _context.Categories
                .Select(c => new SelectListItem
                {
                    Value = c.CategoryId.ToString(),
                    Text = c.Name
                }).ToListAsync();

            var viewModel = new JobFormViewModel
            {
                OfferId = job.OfferId,
                Title = job.Title,
                Description = job.Description,
                Salary = job.Salary,
                CategoryId = job.CategoryId,
                Categories = categories
            };

            return View(viewModel);
        }

        public async Task<IActionResult> DeleteJob(int id)
        {
            var job = await _context.Offers.FindAsync(id);
            if (job == null)
            {
                TempData["ErrorMessage"] = "Job offer not found!";
                return RedirectToAction(nameof(Dashboard));
            }

            _context.Offers.Remove(job);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Job offer deleted successfully!";
            return RedirectToAction(nameof(Dashboard));
        }

    }
}
