using JobListingSite.Data.Entities;
using JobListingSite.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Data;
using X.PagedList;

namespace JobListingSite.Web.Controllers
{
    [Authorize(Roles = "HR")]
    public class HRController : Controller
    {
        private readonly JobListingDbContext _context;

        public HRController(JobListingDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "HR")]
        public async Task<IActionResult> ManageJobs(int page = 1)
        {
            var pageSize = 10; // Set the page size for pagination
            var jobOffers = await _context.Offers.Include(o => o.Category)
                                                  .OrderBy(o => o.Title) // You can customize this sorting
                                                  .ToPagedListAsync(page, pageSize);
            return View(jobOffers);
        }

        // GET: Create job form
        public IActionResult CreateJob()
        {
            // Get categories from the database
            var categories = _context.Categories.ToList();

            // Convert the List<Category> to a SelectList of SelectListItems
            var categorySelectList = categories.Select(c => new SelectListItem
            {
                Value = c.CategoryId.ToString(),
                Text = c.Name
            }).ToList();

            // Pass the SelectList to the view
            ViewBag.Categories = categorySelectList;

            return View();
        }

        // Add job to database
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateJob(Offer model)
        {
            if (ModelState.IsValid)
            {
                _context.Offers.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Job offer created successfully!";
                return RedirectToAction(nameof(ManageJobs));
            }

            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditJob(int id, Offer model)
        {
            if (id != model.OfferId) return NotFound();

            // Retrieve the existing job offer
            var existingJob = await _context.Offers.FindAsync(id);
            if (existingJob == null) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    existingJob.Title = model.Title;
                    existingJob.Description = model.Description;
                    existingJob.Salary = model.Salary;
                    existingJob.CategoryId = model.CategoryId;

                    // Save changes
                    _context.Update(existingJob);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Job offer updated successfully!";
                    return RedirectToAction(nameof(ManageJobs));
                }
                catch (DbUpdateConcurrencyException)
                {
                    return NotFound();
                }
            }

            // If the model state is invalid, return the form with existing data
            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", model.CategoryId);
            return View(model);
        }

        public async Task<IActionResult> EditJob(int id)
        {
            var job = await _context.Offers.FindAsync(id);
            if (job == null) return NotFound();

            // Convert categories into SelectListItems
            var categories = _context.Categories.ToList();
            var categorySelectList = categories.Select(c => new SelectListItem
            {
                Value = c.CategoryId.ToString(),
                Text = c.Name
            }).ToList();

            // Pass the SelectList to the view
            ViewBag.Categories = categorySelectList;

            return View(job);
        }


        public async Task<IActionResult> DeleteJob(int id)
        {
            var job = await _context.Offers.FindAsync(id);
            if (job == null) return NotFound();

            _context.Offers.Remove(job);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Job offer deleted successfully!";
            return RedirectToAction(nameof(ManageJobs));
        }

    }
}
