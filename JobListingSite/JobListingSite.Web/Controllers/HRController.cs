using JobListingSite.Data.Entities;
using JobListingSite.Data.Enums;
using JobListingSite.Web.Data;
using JobListingSite.Web.Models.Company;
using JobListingSite.Web.Models.HR;
using JobListingSite.Web.Models.JobListing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Data;
using X.PagedList;
using X.PagedList.Mvc.Core;
using X.PagedList.Extensions;
using Microsoft.AspNetCore.Identity;
using System.Net.Sockets;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace JobListingSite.Web.Controllers
{
    [Authorize(Roles = "HR, Admin")]
    public class HRController : Controller
    {
        private readonly JobListingDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IEmailSender _emailSender;

        public HRController(JobListingDbContext context, UserManager<User> userManager, IEmailSender? emailSender = null)
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
        }


        public async Task<IActionResult> Dashboard(string? search, int? categoryId, int page = 1)
        {
            int pageSize = 7;

            var offersQuery = _context.Offers
                .Include(o => o.Category)
                .OrderByDescending(o => o.CreatedAt)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                offersQuery = offersQuery.Where(o => o.Title.Contains(search));
            }

            if (categoryId.HasValue)
            {
                offersQuery = offersQuery.Where(o => o.CategoryId == categoryId);
            }

            var pagedOffers = offersQuery.ToPagedList(page, pageSize);

            var model = new HRDashboardViewModel
            {
                TotalJobs = await _context.Offers.CountAsync(),
                TotalApplications = await _context.JobApplications.CountAsync(),
                PendingApplications = await _context.JobApplications.CountAsync(a => a.Status == ApplicationStatus.Pending),
                ApprovedApplications = await _context.JobApplications.CountAsync(a => a.Status == ApplicationStatus.Approved),
                RejectedApplications = await _context.JobApplications.CountAsync(a => a.Status == ApplicationStatus.Rejected),
                RecentOffers = pagedOffers,
                CurrentPage = page,
                TotalPages = pagedOffers.PageCount,
                SearchQuery = search,
                SelectedCategoryId = categoryId,
                AllCategories = await _context.Categories
                    .Select(c => new SelectListItem
                    {
                        Value = c.CategoryId.ToString(),
                        Text = c.Name
                    }).ToListAsync()
            };

            var pendingCounts = await _context.JobApplications
                    .Where(a => a.Status == ApplicationStatus.Pending)
                    .GroupBy(a => a.OfferId)
                    .Select(g => new { OfferId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.OfferId, x => x.Count);

            model.PendingApplicationsPerOffer = pendingCounts;

            var hrUserId = _userManager.GetUserId(User);

            ViewBag.NewReplies = await _context.HRTickets
        .CountAsync(t =>
            t.CreatedById == hrUserId
            && !t.IsReadByHR
            && t.AdminReply != null
        );

            return View(model);
        }

        public IActionResult ViewApplications(int offerId, string? statusFilter, int page = 1)
        {
            const int pageSize = 10;

            var offer = _context.Offers
                .Include(o => o.Category)
                .FirstOrDefault(o => o.OfferId == offerId);

            if (offer == null) return NotFound();

            var applicationsQuery = _context.JobApplications
                .Include(a => a.User).ThenInclude(u => u.Profile)
                .Where(a => a.OfferId == offerId)
                .OrderByDescending(a => a.AppliedOn)
                .ToList();

            var applications = applicationsQuery.Select(a => new ApplicationViewModel
            {
                Id = a.Id,
                ApplicantName = a.User.Name,
                ApplicantEmail = a.User.Email,
                Status = a.Status,
                AppliedOn = a.AppliedOn,
                ResumeFilePath = a.User.Profile?.ResumeFilePath,
                ProfileImageUrl = a.User.Profile?.ProfileImageUrl ?? a.User.Profile?.SelectedAvatar,
                ApplicantId = a.UserId
            });

            if (!string.IsNullOrEmpty(statusFilter) &&
                Enum.TryParse<ApplicationStatus>(statusFilter, out var parsedStatus))
            {
                applications = applications.Where(a => a.Status == parsedStatus);
                ViewBag.Status = statusFilter;
            }

            var viewModel = new JobApplicationsViewModel
            {
                OfferId = offer.OfferId,
                OfferTitle = offer.Title,
                ApplicationsPaged = applications.ToPagedList(page, pageSize)
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditJob(JobFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Categories = await _context.Categories
                    .Select(c => new SelectListItem
                    {
                        Value = c.CategoryId.ToString(),
                        Text = c.Name
                    }).ToListAsync();
                return View(model);
            }

            var offer = await _context.Offers.FindAsync(model.OfferId);
            if (offer == null)
            {
                TempData["ErrorMessage"] = "Job offer not found!";
                return RedirectToAction(nameof(Dashboard));
            }

            offer.Title = model.Title;
            offer.Description = model.Description;
            offer.Salary = model.Salary;
            offer.CategoryId = model.CategoryId;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Job offer updated successfully!";
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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


        [Authorize(Roles = "HR")]
        [HttpGet]
        public IActionResult CreateTicket()
        {
            return View();
        }

        [Authorize(Roles = "HR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTicket(CreateTicketViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);

            var ticket = new HRTicket
            {
                Title = model.Title,
                Description = model.Description,
                Priority = model.Priority,
                Status = TicketStatus.Open,
                CreatedAt = DateTime.UtcNow,
                CreatedById = user.Id
            };

            _context.HRTickets.Add(ticket);
            await _context.SaveChangesAsync();

            TempData["TicketCreated"] = true;
            TempData["NewTicketId"] = ticket.Id;

            return RedirectToAction("CreatedTicket");
        }

        [HttpGet]
        public async Task<IActionResult> CreatedTicket(int page = 1)
        {
            var hr = await _userManager.GetUserAsync(User);
            var replied = await _context.HRTickets
                .Where(t => t.CreatedById == hr.Id && !t.IsReadByHR && t.AdminReply != null)
                .ToListAsync();
            replied.ForEach(t => t.IsReadByHR = true);
            if (replied.Any()) await _context.SaveChangesAsync();

            var tickets = _context.HRTickets
                .Where(t => t.CreatedById == hr.Id)
                .OrderByDescending(t => t.CreatedAt)
                .ToPagedList(page, 5);

            return View(tickets);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveTicket(int id)
        {
            var ticket = await _context.HRTickets.FindAsync(id);
            if (ticket == null) return NotFound();

            ticket.Status = TicketStatus.Resolved;
            ticket.ResolvedAt = DateTime.UtcNow;
            ticket.IsReadByHR = true;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Ticket marked as resolved!";
            return RedirectToAction(nameof(CreatedTicket));
        }

        [HttpGet]
        public async Task<IActionResult> EditTicket(int id)
        {
            var userId = _userManager.GetUserId(User);
            var ticket = await _context.HRTickets
                .FirstOrDefaultAsync(t => t.Id == id && t.CreatedById == userId);
            if (ticket == null) return NotFound();

            var vm = new EditTicketViewModel
            {
                Id = ticket.Id,
                Title = ticket.Title,
                Description = ticket.Description,
                Priority = ticket.Priority
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTicket(EditTicketViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userId = _userManager.GetUserId(User);
            var ticket = await _context.HRTickets
                .FirstOrDefaultAsync(t => t.Id == model.Id && t.CreatedById == userId);
            if (ticket == null) return NotFound();

            ticket.Title = model.Title;
            ticket.Description = model.Description;
            ticket.Priority = model.Priority;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(CreatedTicket));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReplyToTicket(int id, string reply)
        {
            var userId = _userManager.GetUserId(User);
            var ticket = await _context.HRTickets
                .FirstOrDefaultAsync(t => t.Id == id && t.CreatedById == userId);
            if (ticket == null) return NotFound();

            ticket.HRReply = reply;
            ticket.HRRepliedAt = DateTime.UtcNow;
            ticket.IsReadByAdmin = false;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(CreatedTicket));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTicket(int id)
        {
            var ticket = await _context.HRTickets.FindAsync(id);
            if (ticket == null)
            {
                TempData["ErrorMessage"] = "Ticket not found.";
                return RedirectToAction(nameof(CreatedTicket));
            }

            _context.HRTickets.Remove(ticket);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Ticket deleted successfully!";
            return RedirectToAction(nameof(CreatedTicket));
        }

       
    }
}