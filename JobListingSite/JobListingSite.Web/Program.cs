using JobListingSite.Data.Entities;
using JobListingSite.Web.Data;
using JobListingSite.Web.Models.DataSedeer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Amazon.S3;
using Amazon;
using Amazon.Runtime;

var builder = WebApplication.CreateBuilder(args);

// ✅ Load AWS credentials from appsettings.json manually
var awsOptions = builder.Configuration.GetSection("AWS");
var awsCredentials = new BasicAWSCredentials(
    awsOptions["AccessKey"],
    awsOptions["SecretKey"]
);

// ✅ Register the Amazon S3 service with credentials
builder.Services.AddSingleton<IAmazonS3>(sp =>
    new AmazonS3Client(awsCredentials, RegionEndpoint.GetBySystemName(awsOptions["Region"]))
);

// ✅ DB setup
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<JobListingDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ✅ Identity setup
builder.Services.AddDefaultIdentity<User>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<JobListingDbContext>()
.AddDefaultTokenProviders();

// ✅ Role policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireHRRole", policy => policy.RequireRole("HR"));
});

// ✅ MVC + Razor setup
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// ✅ Seed roles/admin user/categories/offers
using (var scope = builder.Services.BuildServiceProvider().CreateScope())
{
    var services = scope.ServiceProvider;
    await DataSeeder.SeedRolesAndAdminAsync(services);
}

// ✅ Middleware setup
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();
app.UseStatusCodePagesWithRedirects("/Account/AccessDenied");

app.Run();
