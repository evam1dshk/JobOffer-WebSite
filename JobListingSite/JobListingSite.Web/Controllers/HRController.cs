using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace JobListingSite.Web.Controllers
{
    [Authorize(Roles = "HR")]
    public class HRController : Controller
    {
        public IActionResult ManageJobs()
        {
            return View();
        }

    }
}
