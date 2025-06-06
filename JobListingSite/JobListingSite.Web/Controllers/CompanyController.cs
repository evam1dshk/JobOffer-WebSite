﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobListingSite.Web.Data;
using JobListingSite.Data.Entities;
using JobListingSite.Web.Models.Company;
using JobListingSite.Web.Models.JobListing;
using Microsoft.AspNetCore.Mvc.Rendering;
using JobListingSite.Data.Enums;
using Microsoft.AspNetCore.Identity.UI.Services;
using X.PagedList;
using X.PagedList.Extensions;
using System;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JobListingSite.Web.Controllers
{
    [Authorize(Roles = "Company")]
    public class CompanyController : Controller
    {
        private readonly JobListingDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;

        public CompanyController(JobListingDbContext context, UserManager<User> userManager, IWebHostEnvironment hostEnvironment, IConfiguration configuration, IEmailSender emailSender)
        {
            _context = context;
            _userManager = userManager;
            _hostEnvironment = hostEnvironment;
            _configuration = configuration;
            _emailSender = emailSender;
        }

        private async Task<string> UploadLogoLocallyAsync(IFormFile logo)
        {
            if (logo == null || logo.Length == 0)
                throw new Exception("Invalid logo file.");

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/logos");

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder); 

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(logo.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await logo.CopyToAsync(stream);
            }

            return "/uploads/logos/" + uniqueFileName;
        }



        [HttpGet]
        [AllowAnonymous]
        public IActionResult Profile(string id, string? returnTo)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var company = _context.Users
                .Include(u => u.CompanyProfile)
                .FirstOrDefault(u => u.Id == id && u.IsCompany && u.CompanyProfile != null);

            if (company == null)
                return NotFound();

            ViewData["ReturnTo"] = returnTo;
            return View(company);
        }

        [HttpGet]
        [Authorize(Roles = "Company")]
        public async Task<IActionResult> ManageProfile()
        {
            var userId = _userManager.GetUserId(User);
            var user = await _context.Users
                .Include(u => u.CompanyProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || user.CompanyProfile == null)
                return NotFound();

            var vm = new CompanyProfileFormViewModel
            {
                CompanyName = user.CompanyProfile.CompanyName,
                Description = user.CompanyProfile.Description,
                Industry = user.CompanyProfile.Industry,
                ContactEmail = user.CompanyProfile.ContactEmail ?? user.Email,
                Phone = user.CompanyProfile.Phone,
                CompanyWebsite = user.CompanyProfile.CompanyWebsite,
                FoundedDate = user.CompanyProfile.FoundedDate,
                Location = user.CompanyProfile.Location,
                LinkedIn = user.CompanyProfile.LinkedIn,
                Twitter = user.CompanyProfile.Twitter,
                NumberOfEmployees = user.CompanyProfile.NumberOfEmployees,
                LogoUrl = user.CompanyProfile.LogoUrl,
            };

            return View(vm);
        }
        [HttpPost]
        [Authorize(Roles = "Company")]
        public async Task<IActionResult> ManageProfile(CompanyProfileFormViewModel model)
        {
            var userId = _userManager.GetUserId(User);
            var user = await _context.Users
                .Include(u => u.CompanyProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || user.CompanyProfile == null)
            {
                TempData["ErrorMessage"] = "Company profile not found.";
                return NotFound();
            }

            foreach (var key in ModelState.Keys)
            {
                var state = ModelState[key];
                foreach (var error in state.Errors)
                {
                    Console.WriteLine($"⚠️ Validation error on {key}: {error.ErrorMessage}");
                }
            }


            if (!ModelState.IsValid)
            {
                // Log validation errors to console
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine("❌ Validation error: " + error.ErrorMessage);
                }

                return View(model);
            }

            // ✅ Update logo only if file is provided
            if (model.Logo != null)
            {
                user.CompanyProfile.LogoUrl = await UploadLogoLocallyAsync(model.Logo);
            }

            // ✅ Update profile fields
            user.CompanyProfile.CompanyName = model.CompanyName;
            user.CompanyProfile.Description = model.Description;
            user.CompanyProfile.Industry = model.Industry;
            user.CompanyProfile.Phone = model.Phone;
            user.CompanyProfile.ContactEmail = model.ContactEmail;
            user.CompanyProfile.CompanyWebsite = model.CompanyWebsite;
            user.CompanyProfile.FoundedDate = model.FoundedDate;
            user.CompanyProfile.Location = model.Location;
            user.CompanyProfile.LinkedIn = model.LinkedIn;
            user.CompanyProfile.Twitter = model.Twitter;
            user.CompanyProfile.NumberOfEmployees = model.NumberOfEmployees;

            // ✅ Save to database
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Company profile updated successfully!";
            return RedirectToAction("ManageProfile");
        }


        [Authorize(Roles = "Company")]
        public async Task<IActionResult> ManageJobs(int page = 1)
        {
            var userId = _userManager.GetUserId(User);

            var jobs = _context.Offers
             .Where(o => o.CompanyId == userId)
            .Include(o => o.Category)
            .OrderByDescending(o => o.CreatedAt)
            .ToPagedList(page, 5);
            return View(jobs);
        }

        [HttpGet]
        [Authorize(Roles = "Company")]
        public IActionResult AddJob()
        {
            var categories = _context.Categories
                .Select(c => new SelectListItem
                {
                    Value = c.CategoryId.ToString(),
                    Text = c.Name
                }).ToList();

            var model = new JobFormViewModel
            {
                Categories = categories
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Company")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddJob(JobFormViewModel model)
        {
            var userId = _userManager.GetUserId(User);
            var company = await _context.Users.Include(c => c.CompanyProfile).FirstOrDefaultAsync(c => c.Id == userId);

            if (company == null || company.CompanyProfile == null)
                return Unauthorized();

            if (!ModelState.IsValid)
            {
                model.Categories = _context.Categories.Select(c => new SelectListItem
                {
                    Value = c.CategoryId.ToString(),
                    Text = c.Name
                }).ToList();
                return View(model);
            }

            var newJob = new Offer
            {
                Title = model.Title,
                Description = model.Description,
                Salary = model.Salary,
                CategoryId = model.CategoryId,
                CompanyId = userId,
                Location = model.Location,
                CreatedAt = DateTime.UtcNow
            };

            _context.Offers.Add(newJob);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Job offer posted successfully!";
            return RedirectToAction("ManageJobs");
        }


        [HttpGet]
        [Authorize(Roles = "Company")]
        public async Task<IActionResult> EditJob(int id)
        {
            var job = await _context.Offers.FirstOrDefaultAsync(j => j.OfferId == id && j.CompanyId == _userManager.GetUserId(User));
            if (job == null) return NotFound();

            var model = new JobFormViewModel
            {
                OfferId = job.OfferId,
                Title = job.Title,
                Description = job.Description,
                Salary = job.Salary,
                CategoryId = job.CategoryId,
                Location = job.Location,
                Categories = _context.Categories.Select(c => new SelectListItem
                {
                    Value = c.CategoryId.ToString(),
                    Text = c.Name
                }).ToList()
            };

            return View("AddJob", model); 
        }

        [HttpPost]
        [Authorize(Roles = "Company")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditJob(JobFormViewModel model)
        {
            var userId = _userManager.GetUserId(User);
            var job = await _context.Offers
                .FirstOrDefaultAsync(j => j.OfferId == model.OfferId && j.CompanyId == userId);

            if (job == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                model.Categories = _context.Categories.Select(c => new SelectListItem
                {
                    Value = c.CategoryId.ToString(),
                    Text = c.Name
                }).ToList();

                return View("AddJob", model);
            }

            job.Title = model.Title;
            job.Description = model.Description;
            job.Salary = model.Salary;
            job.CategoryId = model.CategoryId;
            job.Location = model.Location;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Job updated successfully!";
            return RedirectToAction("ManageJobs");
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteJob(int id)
        {
            var job = await _context.Offers.FirstOrDefaultAsync(j => j.OfferId == id && j.CompanyId == _userManager.GetUserId(User));
            if (job == null) return NotFound();

            _context.Offers.Remove(job);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Job deleted successfully!";
            return RedirectToAction("ManageJobs");
        }

        [Authorize(Roles = "Company")]
        public async Task<IActionResult> ViewApplications(int id, string? statusFilter, int page = 1)
        {
            var userId = _userManager.GetUserId(User);

            var offer = await _context.Offers
                .Include(o => o.JobApplications)
                    .ThenInclude(a => a.User)
                        .ThenInclude(u => u.Profile) 
                .FirstOrDefaultAsync(o => o.OfferId == id && o.CompanyId == userId);

            if (offer == null) return NotFound();

            var applications = offer.JobApplications
                .Where(a => string.IsNullOrEmpty(statusFilter) || a.Status.ToString() == statusFilter)
                .Select(a => new ApplicationViewModel
                {
                    Id = a.Id,
                    ApplicantId = a.User.Id,
                    ApplicantName = a.User.Name,
                    ApplicantEmail = a.User.Email,
                    AppliedOn = a.AppliedOn,
                    Status = a.Status,
                    ResumeFilePath = a.User.Profile?.ResumeFilePath,
                    ProfilePictureUrl = a.User.Profile?.ProfileImageUrl ?? a.User.Profile?.SelectedAvatar
                })
                .OrderByDescending(a => a.AppliedOn)
                .ToPagedList(page, 5);

            var viewModel = new JobApplicationsViewModel
            {
                OfferId = offer.OfferId,
                OfferTitle = offer.Title,
                Applications = applications.ToList(),
                ApplicationsPaged = applications
            };

            return View(viewModel);
        }

        [HttpPost]
        [Authorize(Roles = "Company")]
        public async Task<IActionResult> ApproveApplication(int applicationId)
        {
            var userId = _userManager.GetUserId(User);

            var app = await _context.JobApplications
                .Include(a => a.User)
                .Include(a => a.Offer)
                .FirstOrDefaultAsync(a => a.Id == applicationId && a.Offer.CompanyId == userId);

            if (app == null || app.Status != ApplicationStatus.Pending)
                return NotFound();

            app.Status = ApplicationStatus.Approved;
            await _context.SaveChangesAsync();

            await _emailSender.SendEmailAsync(
                app.User.Email,
                $"Application Update for {app.Offer.Title}",
                $"Your application for <strong>{app.Offer.Title}</strong> has been <span style='color:green;font-weight:bold'>APPROVED</span>.<br><br>Thank you for using JobListingSite!"
            );

            TempData["Success"] = "Application approved and email notification sent!";
            return RedirectToAction("ViewApplications", new { id = app.OfferId }); // ✅ fixed
        }

        [HttpPost]
        [Authorize(Roles = "Company")]
        public async Task<IActionResult> RejectApplication(int applicationId)
        {
            var userId = _userManager.GetUserId(User);

            var app = await _context.JobApplications
                .Include(a => a.User)
                .Include(a => a.Offer)
                .FirstOrDefaultAsync(a => a.Id == applicationId && a.Offer.CompanyId == userId);

            if (app == null || app.Status != ApplicationStatus.Pending)
                return Forbid();

            app.Status = ApplicationStatus.Rejected;
            await _context.SaveChangesAsync();

            await _emailSender.SendEmailAsync(
                app.User.Email,
                $"Application Update for {app.Offer.Title}",
                $"Your application for <strong>{app.Offer.Title}</strong> has been <span style='color:red;font-weight:bold'>REJECTED</span>.<br><br>Thank you for using JobListingSite."
            );

            TempData["Warning"] = "Application rejected and email notification sent.";
            return RedirectToAction("ViewApplications", new { id = app.OfferId }); 
        }
    }
}
