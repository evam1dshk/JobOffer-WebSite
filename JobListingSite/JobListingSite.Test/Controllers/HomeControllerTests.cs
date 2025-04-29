using JobListingSite.Data.Entities;
using JobListingSite.Web.Controllers;
using JobListingSite.Web.Data;
using JobListingSite.Web.Models;
using JobListingSite.Web.Models.Home;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobListingSite.Test.Controllers
{
    public class HomeControllerTests
    {
        private HomeController _controller;
        private Mock<UserManager<User>> _userManagerMock;
        private JobListingDbContext _context;

        [SetUp]
        public void Setup()
        {
            var store = new Mock<IUserStore<User>>();
            _userManagerMock = new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);

            var options = new DbContextOptionsBuilder<JobListingDbContext>()
                .UseInMemoryDatabase(databaseName: "JobListingTestDb")
                .Options;

            _context = new JobListingDbContext(options);
            _controller = new HomeController(_userManagerMock.Object, _context);

            var httpContext = new DefaultHttpContext();
            httpContext.TraceIdentifier = Guid.NewGuid().ToString();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }


        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted(); 
            _context.Dispose();
        }

        [Test]
        public async Task Index_ReturnsViewResult_WithHomeViewModel()
        {
            var company = new User { Id = "company123", Name = "Test Company" };
            var category = new Category { CategoryId = 1, Name = "IT" };
            var offer = new Offer
            {
                OfferId = 1,
                Title = "Test Offer",
                Description = "This is a test description for a job offer.",
                Company = company,
                Category = category,
                CreatedAt = DateTime.UtcNow
            };

            _context.Offers.Add(offer);
            await _context.SaveChangesAsync();

            var result = await _controller.Index();

            Assert.That(result, Is.InstanceOf<ViewResult>());

            var viewResult = result as ViewResult;
            Assert.That(result, Is.InstanceOf<ViewResult>());

            var model = viewResult.Model as HomeViewModel;
            Assert.That(viewResult.Model, Is.Not.Null);
            Assert.That(model.FeaturedJobs.Any(), Is.True);
            Assert.That(model.FeaturedJobs.First().Title, Is.EqualTo("Test Offer"));
        }

        [Test]
        public void About_ReturnsViewResult()
        {
            var result = _controller.About();

            Assert.That(result, Is.InstanceOf<ViewResult>());
        }

        [Test]
        public void Error_ReturnsViewResult_WithErrorViewModel()
        {
            // Act
            var result = _controller.Error();

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());

            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null); 

            var model = viewResult.Model;
            Assert.That(model, Is.InstanceOf<ErrorViewModel>());
        }
    }
}
