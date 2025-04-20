using JobListingSite.Data.Entities;
using JobListingSite.Web.Data;
using JobListingSite.Web.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using X.PagedList;
using X.PagedList.Extensions;

namespace JobListingSite.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly JobListingDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;


        public AdminController(JobListingDbContext context, UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }
        public async Task<IActionResult> Dashboard()
        {
            var totalUsers = await _context.Users.CountAsync();
            var totalCompanies = await _context.Users.CountAsync(u => u.IsCompany);
            var totalJobs = await _context.Offers.CountAsync();
            var totalCategories = await _context.Categories.CountAsync();

            // 📈 Calculate new job offers added per day (last 7 days)
            var oneWeekAgo = DateTime.UtcNow.AddDays(-7);

            var newOffersByDay = await _context.Offers
                .Where(o => o.CreatedAt >= oneWeekAgo)
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalCompanies = totalCompanies;
            ViewBag.TotalJobs = totalJobs;
            ViewBag.TotalCategories = totalCategories;

            ViewBag.NewOffersByDay = newOffersByDay;

            return View();
        }

        public async Task<IActionResult> ManageCategories()
        {
            var categories = await _context.Categories.ToListAsync();
            return View(categories);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory(Category category)
        {
            if (!ModelState.IsValid)
            {
                return View(category);
            }

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Category added successfully!";
            return RedirectToAction("ManageCategories");
        }

        public async Task<IActionResult> EditCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound();

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(Category category)
        {
            if (!ModelState.IsValid)
            {
                return View(category);
            }

            _context.Categories.Update(category);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Category updated successfully!";
            return RedirectToAction("ManageCategories");
        }

        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound();

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Category deleted successfully!";
            return RedirectToAction("ManageCategories");
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ManageUsers(int? page)
        {
            var users = await _userManager.Users.ToListAsync();
            var roles = await _roleManager.Roles.ToListAsync();

            var userViewModels = new List<UserViewModel>();

            foreach (var user in users)
            {
                var userRoles = await _userManager.GetRolesAsync(user);

                userViewModels.Add(new UserViewModel
                {
                    UserId = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    Role = userRoles.FirstOrDefault() ?? "None",
                    IsLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow
                });
            }

            int pageSize = 7;
            int pageNumber = page ?? 1;

            var pagedUsers = userViewModels.ToPagedList(pageNumber, pageSize);

            return View(pagedUsers);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PromoteUser(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("ManageUsers");
            }

            if (await _userManager.IsInRoleAsync(user, roleName))
            {
                TempData["WarningMessage"] = "User is already in this role.";
                return RedirectToAction("ManageUsers");
            }

            var currentRoles = await _userManager.GetRolesAsync(user);

            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            await _userManager.AddToRoleAsync(user, roleName);

            TempData["SuccessMessage"] = $"User {user.Email} promoted to {roleName}.";
            return RedirectToAction("ManageUsers");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DemoteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("ManageUsers");
            }

            var currentRoles = await _userManager.GetRolesAsync(user);

            if (currentRoles.Contains("Admin"))
            {
                TempData["ErrorMessage"] = "Cannot demote another Admin!";
                return RedirectToAction("ManageUsers");
            }

            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, "Registered");

            TempData["SuccessMessage"] = $"User {user.Email} demoted to Registered.";
            return RedirectToAction("ManageUsers");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LockUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("ManageUsers");
            }

            await _userManager.SetLockoutEndDateAsync(user, DateTime.UtcNow.AddYears(100));
            TempData["SuccessMessage"] = $"User {user.Email} locked successfully.";

            return RedirectToAction(nameof(ManageUsers));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("ManageUsers");
            }

            await _userManager.SetLockoutEndDateAsync(user, null);
            TempData["SuccessMessage"] = $"User {user.Email} unlocked successfully.";

            return RedirectToAction(nameof(ManageUsers));
        }

        [HttpGet]
        public IActionResult CreateCategory()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(Category category)
        {
            if (ModelState.IsValid)
            {
                _context.Categories.Add(category);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Category created successfully!";
                return RedirectToAction(nameof(ManageCategories));
            }

            TempData["ErrorMessage"] = "Something went wrong while creating the category.";
            return View(category);
        }

        [HttpGet]
        public async Task<IActionResult> ChangeRole(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(ManageUsers));
            }

            var currentRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "None";

            var availableRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

            var model = new ChangeRoleViewModel
            {
                UserId = user.Id,
                UserName = user.UserName,
                CurrentRole = currentRole,
                AvailableRoles = availableRoles
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(ChangeRoleViewModel model)
        {
            if (string.IsNullOrEmpty(model.NewRole))
            {
                // Load roles again because View needs it even after error
                model.AvailableRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
                TempData["ErrorMessage"] = "Please select a new role.";
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(ManageUsers));
            }

            var currentRoles = await _userManager.GetRolesAsync(user);

            if (currentRoles.Contains(model.NewRole))
            {
                TempData["WarningMessage"] = "User already has this role.";
                return RedirectToAction(nameof(ManageUsers));
            }

            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, model.NewRole);

            TempData["SuccessMessage"] = $"Role changed to {model.NewRole} successfully!";
            return RedirectToAction(nameof(ManageUsers));
        }


        [HttpGet]
        public async Task<IActionResult> ViewCompanies()
        {
            var companies = await _context.CompanyProfiles
                .Include(c => c.User)
                .ToListAsync();

            return View(companies);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveCompany(int id)
        {
            var company = await _context.CompanyProfiles.FindAsync(id);

            if (company == null)
            {
                TempData["ErrorMessage"] = "Company not found.";
                return RedirectToAction(nameof(ViewCompanies));
            }

            company.IsVerified = true;
            _context.CompanyProfiles.Update(company);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Company '{company.CompanyName}' approved successfully!";
            return RedirectToAction(nameof(ViewCompanies));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCompany(int id)
        {
            var company = await _context.CompanyProfiles.FindAsync(id);

            if (company == null)
            {
                TempData["ErrorMessage"] = "Company not found.";
                return RedirectToAction(nameof(ViewCompanies));
            }

            _context.CompanyProfiles.Remove(company);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Company '{company.CompanyName}' deleted successfully!";
            return RedirectToAction(nameof(ViewCompanies));
        }

        [HttpGet]
        public async Task<IActionResult> ViewCompanyProfile(int id)
        {
            var company = await _context.CompanyProfiles
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (company == null)
            {
                TempData["ErrorMessage"] = "Company not found.";
                return RedirectToAction(nameof(ViewCompanies));
            }

            return View(company); 
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