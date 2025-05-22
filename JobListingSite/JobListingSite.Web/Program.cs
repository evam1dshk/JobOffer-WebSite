using JobListingSite.Data.Entities;
using JobListingSite.Web.Data;
using JobListingSite.Web.Models.DataSedeer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Amazon.S3;
using Amazon;
using Amazon.Runtime;
using JobListingSite.Web.Models.Mail;
using Microsoft.AspNetCore.Identity.UI.Services;

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
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();



// ✅ Identity setup
builder.Services.AddDefaultIdentity<User>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = true;
    options.Password.RequiredLength = 6;
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

using (var scope = builder.Services.BuildServiceProvider().CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<JobListingDbContext>();
    dbContext.Database.Migrate();
    await DataSeeder.SeedRolesAndAdminAsync(services);
}

builder.Services.ConfigureApplicationCookie(options =>
{
    options.AccessDeniedPath = "/AccessDenied"; // <- simpler path
});


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

app.Run();
