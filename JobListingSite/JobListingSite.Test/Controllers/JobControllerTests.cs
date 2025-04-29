using JobListingSite.Data.Entities;
using JobListingSite.Data.Enums;
using JobListingSite.Web.Controllers;
using JobListingSite.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity.UI.Services;
using Moq;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace JobListingSite.Test.Controllers
{
    [TestFixture]
    public class JobControllerTests
    {
        private JobController _controller;
        private JobListingDbContext _context;
        private Mock<UserManager<User>> _userManagerMock;
        private Mock<IEmailSender> _emailSenderMock;

        [SetUp]
        public void Setup()
        {
            // Setup InMemory Database
            var options = new DbContextOptionsBuilder<JobListingDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new JobListingDbContext(options);

            var userStoreMock = new Mock<IUserStore<User>>();
            _userManagerMock = new Mock<UserManager<User>>(userStoreMock.Object, null, null, null, null, null, null, null, null);
            _emailSenderMock = new Mock<IEmailSender>();

            _controller = new JobController(_context, _userManagerMock.Object, _emailSenderMock.Object);

            var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
            _controller.TempData = tempData;
        }

        [Test]
        public void Browse_ReturnsViewResult()
        {
            var result = _controller.Browse(null, null, 1);

            Assert.That(result, Is.InstanceOf<ViewResult>());
        }

        [Test]
        public async Task Details_ReturnsNotFound_WhenJobDoesNotExist()
        {
            var result = await _controller.Details(999, null);

            // Assert
            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task Details_ReturnsViewResult_WhenJobExists()
        {
            var company = new User
            {
                Id = "company1",
                UserName = "CompanyTest",
                CompanyProfile = new CompanyProfile
                {
                    CompanyName = "Test Company",
                    ContactEmail = "company@test.com",
                }
            };
            _context.Users.Add(company);

            var category = new Category { CategoryId = 1, Name = "IT" };
            _context.Categories.Add(category);

            var offer = new Offer
            {
                OfferId = 1,
                Title = "Test Job",
                Description = "This is a test job description.",
                Company = company,
                CreatedAt = DateTime.UtcNow,
                Category = category
            };
            _context.Offers.Add(offer);
            await _context.SaveChangesAsync();

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            var result = await _controller.Details(1, null);

            Assert.That(result, Is.InstanceOf<ViewResult>());
        }


        [Test]
        public async Task Apply_ReturnsRedirect_WhenAlreadyApplied()
        {
            var user = new User { Id = "user1", UserName = "TestUser" };
            _context.Users.Add(user);

            var company = new User
            {
                Id = "company1",
                UserName = "CompanyTest",
                CompanyProfile = new CompanyProfile
                {
                    CompanyName = "Test Company",
                    ContactEmail = "company@test.com",
                    Industry = "IT"
                }
            };
            _context.Users.Add(company);

            var job = new Offer
            {
                OfferId = 1,
                Title = "Test Job",
                Description = "This is a test job description.",
                Company = company,
                CreatedAt = DateTime.UtcNow
            };
            _context.Offers.Add(job);

            var application = new JobApplication
            {
                OfferId = 1,
                UserId = "user1",
                AppliedOn = DateTime.UtcNow
            };
            _context.JobApplications.Add(application);

            await _context.SaveChangesAsync();

            _userManagerMock.Setup(um => um.GetUserId(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .Returns("user1");

            var result = await _controller.Apply(1);

            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            Assert.That(_controller.TempData["WarningMessage"], Is.Not.Null);
        }

        [Test]
        public async Task Apply_CreatesApplication_WhenFirstTime()
        {
            var user = new User { Id = "user2", UserName = "TestUser2" };
            _context.Users.Add(user);

            var company = new User
            {
                Id = "company1",
                UserName = "TestCompany",
                CompanyProfile = new CompanyProfile
                {
                    ContactEmail = "company@test.com",
                    CompanyName = "Test Company Inc."
                }
            };
            _context.Users.Add(company);

            var offer = new Offer
            {
                OfferId = 2,
                Title = "New Job",
                Description = "This is a new job description.",  
                Company = company,
                CreatedAt = DateTime.UtcNow
            }; _context.Offers.Add(offer);

            await _context.SaveChangesAsync();

            _userManagerMock.Setup(um => um.GetUserId(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .Returns("user2");

            var result = await _controller.Apply(2);

            var app = await _context.JobApplications.FirstOrDefaultAsync(a => a.UserId == "user2" && a.OfferId == 2);
            Assert.That(app, Is.Not.Null);
            Assert.That(app.Status, Is.EqualTo(ApplicationStatus.Pending));
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            _emailSenderMock.Verify(x => x.SendEmailAsync("company@test.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            Assert.That(_controller.TempData["SuccessMessage"], Is.Not.Null);
        }
    }
}
