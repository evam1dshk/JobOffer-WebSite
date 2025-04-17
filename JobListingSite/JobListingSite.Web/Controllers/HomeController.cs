using JobListingSite.Data.Entities;
using JobListingSite.Web.Data;
using JobListingSite.Web.Models;
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

        public async Task<IActionResult> Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                var userId = _userManager.GetUserId(User);
                var currentUser = await _userManager.FindByIdAsync(userId);

                if (currentUser != null && currentUser.IsCompany)
                {
                    var editRequestCount = await _context.JobEditRequests
                        .Include(r => r.Offer)
                        .CountAsync(r => r.Offer.CompanyId == currentUser.Id);

                    ViewBag.EditRequestCount = editRequestCount;
                }
                else
                {
                    ViewBag.EditRequestCount = 0;
                }
            }
            else
            {
                ViewBag.EditRequestCount = 0;
            }

            return View();
        }
    
        public IActionResult Privacy()
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