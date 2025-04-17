using JobListingSite.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace JobListingSite.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly JobListingDbContext _context;

        public AdminController(JobListingDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            var totalUsers = await _context.Users.CountAsync();
            var totalCompanies = await _context.Users.CountAsync(u => u.IsCompany);
            var totalJobs = await _context.Offers.CountAsync();
            var totalCategories = await _context.Categories.CountAsync();

            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalCompanies = totalCompanies;
            ViewBag.TotalJobs = totalJobs;
            ViewBag.TotalCategories = totalCategories;

            return View();
        }

        public async Task<IActionResult> ManageCategories()
        {
            var categories = await _context.Categories.ToListAsync();
            return View(categories);
        }

        public async Task<IActionResult> ManageUsers()
        {
            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        public IActionResult ViewCompanies()
        {
            return View();
        }

        public IActionResult ViewOffers()
        {
            return View();
        }

        public IActionResult ViewEditRequests()
        {
            return View();
        }
    }
}