using JobListingSite.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace JobListingSite.Web.Data
{
    public class JobListingDbContext : IdentityDbContext<User>
    {
        public JobListingDbContext(DbContextOptions<JobListingDbContext> options)
           : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Offer> Offers { get; set; } = null!;
        public DbSet<Profile> Profiles { get; set; } = null!;
        public DbSet<JobApplication> JobApplications { get; set; } = null!;
        public DbSet<CompanyProfile> CompanyProfiles { get; set; } = null!;
        public DbSet<JobEditRequest> JobEditRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<IdentityRole>().HasData(
            new IdentityRole { Id = "1", Name = "Registered", NormalizedName = "REGISTERED" },
            new IdentityRole { Id = "2", Name = "HR", NormalizedName = "HR" },
            new IdentityRole { Id = "3", Name = "Admin", NormalizedName = "ADMIN" },
            new IdentityRole { Id = "4", Name = "Company", NormalizedName = "COMPANY" }
           );

            // Offer → Category (One-to-Many)
            modelBuilder.Entity<Offer>()
                .HasOne(o => o.Category)
                .WithMany(c => c.Offers)
                .HasForeignKey(o => o.CategoryId);

            // Offer → Company (User) (One-to-Many)
            modelBuilder.Entity<User>()
                .HasMany(u => u.Offers)
                .WithOne(o => o.Company)
                .HasForeignKey(o => o.CompanyId);

            // Profile → User (One-to-One)
            modelBuilder.Entity<User>()
                .HasOne(u => u.Profile)
                .WithOne(p => p.User)
                .HasForeignKey<Profile>(p => p.UserId);

            modelBuilder.Entity<JobApplication>()
                .HasOne(ja => ja.User)
                .WithMany(u => u.JobApplications)
                .HasForeignKey(ja => ja.UserId);

            modelBuilder.Entity<JobApplication>()
                .HasOne(ja => ja.Offer)
                .WithMany(o => o.JobApplications)
                .HasForeignKey(ja => ja.OfferId);

            modelBuilder.Entity<User>()
            .HasOne(u => u.CompanyProfile)
            .WithOne(cp => cp.User)
            .HasForeignKey<CompanyProfile>(cp => cp.UserId);



            base.OnModelCreating(modelBuilder);
        }
    }
}