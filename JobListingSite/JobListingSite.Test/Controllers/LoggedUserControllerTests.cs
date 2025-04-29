using JobListingSite.Web.Controllers;
using JobListingSite.Web.Data;
using JobListingSite.Data.Entities;
using JobListingSite.Data.Enums;
using JobListingSite.Web.Models.LoggedUsers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Hosting;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace JobListingSite.Test.Controllers
{
    [TestFixture]
    public class LoggedUserControllerTests
    {
        private LoggedUserController _controller;
        private JobListingDbContext _context;
        private Mock<UserManager<User>> _userManagerMock;
        private Mock<IWebHostEnvironment> _envMock;

        [SetUp]
        public void Setup()
        {
            // In-memory EF
            var options = new DbContextOptionsBuilder<JobListingDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new JobListingDbContext(options);

            // Identity stubs
            var userStoreMock = new Mock<IUserStore<User>>();
            _userManagerMock = new Mock<UserManager<User>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);
            _envMock = new Mock<IWebHostEnvironment>();

            // Controller
            _controller = new LoggedUserController(
                _userManagerMock.Object,
                _context,
                _envMock.Object
            )
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                },
                // *** wire up TempData so RedirectToAction can set SuccessMessage ***
                TempData = new TempDataDictionary(
                    new DefaultHttpContext(),
                    Mock.Of<ITempDataProvider>()
                )
            };
        }

        [Test]
        public async Task ManageProfile_GET_ReturnsViewResult()
        {
            // Arrange
            var user = new User { Id = "user1", Name = "Test User" };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            _userManagerMock
                .Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(u => u.GetRolesAsync(It.IsAny<User>()))
                .ReturnsAsync(new List<string> { "Registered" });

            // Act
            var result = await _controller.ManageProfile();

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            var vr = (ViewResult)result;
            Assert.That(vr.Model, Is.InstanceOf<LoggedUserProfileViewModel>());
        }

        [Test]
        public async Task ManageProfile_POST_UpdatesProfile()
        {
            // Arrange
            var user = new User { Id = "user1", Name = "Old Name" };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var profile = new Profile { UserId = user.Id, Bio = "old bio" };
            await _context.Profiles.AddAsync(profile);
            await _context.SaveChangesAsync();

            _userManagerMock
                .Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(u => u.UpdateAsync(It.IsAny<User>()))
                .ReturnsAsync(IdentityResult.Success);

            var model = new LoggedUserProfileViewModel
            {
                Name = "New Name",
                Phone = "123456",
                Bio = "New Bio",
                Location = "New York",
                LinkedInUrl = "https://linkedin.com/test",
                PortfolioUrl = "https://portfolio.com/test"
            };

            // Act
            var result = await _controller.ManageProfile(model);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            var updated = await _context.Profiles.FirstAsync(p => p.UserId == user.Id);
            Assert.That(updated.Bio, Is.EqualTo("New Bio"));
        }

        [Test]
        public async Task MyApplications_ReturnsViewResult()
        {
            // Arrange
            var company = new User
            {
                Id = "company1",
                CompanyProfile = new CompanyProfile
                {
                    CompanyName = "Test Co.",
                    ContactEmail = "hr@test.co"
                }
            };
            await _context.Users.AddAsync(company);

            var offer = new Offer
            {
                OfferId = 1,
                Title = "Software Engineer",
                Description = "Test job description",
                Company = company,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Offers.AddAsync(offer);

            var user = new User { Id = "user1" };
            await _context.Users.AddAsync(user);
            var profile = new Profile
            {
                UserId = user.Id,
                ProfileImageUrl = "/img/avatar.png"
            };
            await _context.Profiles.AddAsync(profile);

            var application = new JobApplication
            {
                OfferId = 1,
                UserId = user.Id,
                AppliedOn = DateTime.UtcNow,
                Status = ApplicationStatus.Pending
            };
            await _context.JobApplications.AddAsync(application);
            await _context.SaveChangesAsync();

            _userManagerMock
                .Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns(user.Id);

            var result = await _controller.MyApplications();

            Assert.That(result, Is.InstanceOf<ViewResult>());
            var vr = (ViewResult)result;
            var list = vr.Model as List<MyApplicationsViewModel>;
            Assert.That(list, Is.Not.Null);
            Assert.That(list.Count, Is.EqualTo(1));
            Assert.That(list[0].JobTitle, Is.EqualTo("Software Engineer"));
            Assert.That(list[0].CompanyName, Is.EqualTo("Test Co."));
            Assert.That(list[0].ProfileImageUrl, Is.EqualTo("/img/avatar.png"));
        }

        [Test]
        public async Task PublicProfile_ReturnsNotFound_WhenIdIsNull()
        {
            var result = await _controller.PublicProfile(null);
            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task PublicProfile_ReturnsNotFound_WhenUserNotFound()
        {
            var result = await _controller.PublicProfile("doesnt-exist");
            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task PublicProfile_ReturnsViewResult_WhenUserExists()
        {
            var user = new User
            {
                Id = "user1",
                Name = "Test User",
                Email = "test@example.com",
                Profile = new Profile { Bio = "Bio", Location = "Loc" }
            };
            await _context.Users.AddAsync(user);
            await _context.Profiles.AddAsync(user.Profile);
            await _context.SaveChangesAsync();

            var result = await _controller.PublicProfile("user1");
            Assert.That(result, Is.InstanceOf<ViewResult>());
        }
    }
}
