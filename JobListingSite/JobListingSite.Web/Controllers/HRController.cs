using JobListingSite.Data.Entities;
using JobListingSite.Data.Enums;
using JobListingSite.Web.Data;
using JobListingSite.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Data;
using X.PagedList;

namespace JobListingSite.Web.Controllers
{
    [Authorize(Roles = "HR, Admin" )]
    public class HRController : Controller
    {
        private readonly JobListingDbContext _context;

        public HRController(JobListingDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> ManageJobs(string search, int page = 1)
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

            // Stats
            ViewBag.TotalApplicationsCount = await _context.JobApplications.CountAsync();
            ViewBag.PendingApplicationsCount = await _context.JobApplications
                                                            .Where(a => a.Status == ApplicationStatus.Pending)
                                                            .Where(a => a.Status == ApplicationStatus.Pending)
                                                            .CountAsync();

            var jobOffers = await query.OrderBy(o => o.Title).ToPagedListAsync(page, pageSize);

            ViewBag.SearchQuery = search;
            return View(jobOffers);
        }

        // GET: Create job form
        public IActionResult CreateJob()
        {
            var categories = _context.Categories
                     .Select(c => new SelectListItem
                     {
                         Value = c.CategoryId.ToString(),
                         Text = c.Name
                     })
                     .ToList();

            var viewModel = new JobFormViewModel
            {
                Categories = categories
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateJob(JobFormViewModel model)
        {
            Console.WriteLine("POST CreateJob called"); // Add this

            if (!ModelState.IsValid)
            {
                Console.WriteLine("ModelState is invalid"); // Add this
                model.Categories = _context.Categories
                    .Select(c => new SelectListItem
                    {
                        Value = c.CategoryId.ToString(),
                        Text = c.Name
                    }).ToList();

                return View(model);
            }

            var offer = new Offer
            {
                Title = model.Title,
                Description = model.Description,
                Salary = model.Salary,
                CategoryId = model.CategoryId
            };

            _context.Offers.Add(offer);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Job offer created successfully!";
            return RedirectToAction(nameof(ManageJobs));
        }

        // Example for POST-Edit action

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditJob(int id, JobFormViewModel model)
        {
            if (id != model.OfferId)
            {
                TempData["ErrorMessage"] = "Invalid job offer ID.";
                return RedirectToAction(nameof(ManageJobs));
            }

            var existingJob = await _context.Offers.FindAsync(id);
            if (existingJob == null) return NotFound();

            if (ModelState.IsValid)
            {
                existingJob.Title = model.Title;
                existingJob.Description = model.Description;
                existingJob.Salary = model.Salary;
                existingJob.CategoryId = model.CategoryId;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Job offer updated successfully!";
                return RedirectToAction(nameof(ManageJobs));
            }

            model.Categories = await _context.Categories
                .Select(c => new SelectListItem
                {
                    Value = c.CategoryId.ToString(),
                    Text = c.Name
                }).ToListAsync();

            return View(model);
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
                return RedirectToAction(nameof(ManageJobs));
            }

            _context.Offers.Remove(job);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Job offer deleted successfully!";
            return RedirectToAction(nameof(ManageJobs));
        }
    }
}
