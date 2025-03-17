using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace JobListingSite.Web.Data
{
    public class JobListingDbContext : IdentityDbContext
    {
        public JobListingDbContext(DbContextOptions<JobListingDbContext> options)
            : base(options)
        {
        }
    }
}