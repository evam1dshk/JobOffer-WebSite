using JobListingSite.Web.Controllers;
using JobListingSite.Web.Data;
using JobListingSite.Data.Entities;
using JobListingSite.Data.Enums;
using JobListingSite.Web.Models.Company;
using JobListingSite.Web.Models.JobListing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc.Rendering;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using X.PagedList;

namespace JobListingSite.Test.Controllers
{
    [TestFixture]
    public class CompanyControllerTests
    {
        private CompanyController _controller;
        private JobListingDbContext _ctx;
        private Mock<UserManager<User>> _um;
        private Mock<IWebHostEnvironment> _env;
        private Mock<IConfiguration> _conf;
        private Mock<IEmailSender> _email;

        [SetUp]
        public void Setup()
        {
            var opts = new DbContextOptionsBuilder<JobListingDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _ctx = new JobListingDbContext(opts);

            var store = new Mock<IUserStore<User>>();
            _um = new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);
            _env = new Mock<IWebHostEnvironment>();
            _conf = new Mock<IConfiguration>();
            _email = new Mock<IEmailSender>();

            _controller = new CompanyController(
                _ctx, _um.Object, _env.Object, _conf.Object, _email.Object
            )
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                },
                TempData = new TempDataDictionary(
                    new DefaultHttpContext(),
                    Mock.Of<ITempDataProvider>()
                )
            };
        }

        [Test]
        public void Profile_ReturnsNotFound_WhenIdEmpty()
        {
            var result = _controller.Profile(null, null);
            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public void Profile_ReturnsNotFound_WhenNoCompany()
        {
            var result = _controller.Profile("nope", null);
            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public void Profile_ReturnsView_WhenFound()
        {
            var u = new User
            {
                Id = "c1",
                IsCompany = true,
                CompanyProfile = new CompanyProfile
                {
                    CompanyName = "C",
                    ContactEmail = "c@c"
                }
            };
            _ctx.Users.Add(u);
            _ctx.SaveChanges();

            var vr = _controller.Profile("c1", "x") as ViewResult;
            Assert.That(vr, Is.Not.Null);
            Assert.That(vr.Model, Is.SameAs(u));
            Assert.That(_controller.ViewData["ReturnTo"], Is.EqualTo("x"));
        }

        [Test]
        public async Task ManageProfile_GET_ShowsForm()
        {
            var user = new User
            {
                Id = "c2",
                CompanyProfile = new CompanyProfile
                {
                    CompanyName = "Co",
                    ContactEmail = "co@co"
                }
            };
            _ctx.Users.Add(user);
            await _ctx.SaveChangesAsync();

            _um.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>()))
               .Returns("c2");

            var vr = await _controller.ManageProfile() as ViewResult;
            Assert.That(vr, Is.Not.Null);
            Assert.That(vr.Model, Is.InstanceOf<CompanyProfileFormViewModel>());
        }

        [Test]
        public async Task ManageProfile_POST_SavesAndRedirects()
        {
            var user = new User
            {
                Id = "c3",
                Email = "e@e",
                CompanyProfile = new CompanyProfile
                {
                    CompanyName = "Old",
                    ContactEmail = "e@e"
                }
            };
            _ctx.Users.Add(user);
            await _ctx.SaveChangesAsync();

            _um.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>()))
               .Returns("c3");

            var vm = new CompanyProfileFormViewModel
            {
                CompanyName = "New",
                Description = "D",
                Industry = "I",
                ContactEmail = "e2@e2",
                Phone = "p",
                CompanyWebsite = "w",
                FoundedYear = 2000,
                Location = "L",
                LinkedIn = "li",
                Twitter = "tw",
                NumberOfEmployees = 5
            };

            var result = await _controller.ManageProfile(vm) as RedirectToActionResult;
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ActionName, Is.EqualTo("ManageProfile"));

            var updated = _ctx.Users
                .Include(u => u.CompanyProfile)
                .Single(u => u.Id == "c3")
                .CompanyProfile;
            Assert.That(updated.CompanyName, Is.EqualTo("New"));
            Assert.That(_controller.TempData["SuccessMessage"], Is.Not.Null);
        }

        [Test]
        public async Task ManageJobs_ReturnsPagedList()
        {
            _um.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>()))
               .Returns("c4");

            for (int i = 1; i <= 7; i++)
            {
                _ctx.Offers.Add(new Offer
                {
                    OfferId = i,
                    CompanyId = "c4",
                    Title = $"Job{i}",
                    Description = "Desc",
                    CreatedAt = DateTime.UtcNow
                });
            }
            await _ctx.SaveChangesAsync();

            var vr = await _controller.ManageJobs(1) as ViewResult;
            var list = vr.Model as IPagedList<Offer>;

            Assert.That(list, Is.Not.Null);
            Assert.That(list.TotalItemCount, Is.EqualTo(7));
            Assert.That(list.PageSize, Is.EqualTo(5));
        }

        [Test]
        public void AddJob_GET_PopulatesCategories()
        {
            _ctx.Categories.Add(new Category { CategoryId = 1, Name = "A" });
            _ctx.SaveChanges();

            var vr = _controller.AddJob() as ViewResult;
            var vm = vr.Model as JobFormViewModel;
            Assert.That(vm.Categories, Is.Not.Null);
            Assert.That(vm.Categories.Count(), Is.EqualTo(1));
        }

        [Test]
        public async Task AddJob_POST_WithInvalidModel_ReturnsView()
        {
            var company = new User
            {
                Id = "cX",
                CompanyProfile = new CompanyProfile
                {
                    CompanyName = "TestCo",
                    ContactEmail = "tc@tc"
                }
            };
            _ctx.Users.Add(company);
            await _ctx.SaveChangesAsync();

            _um.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>()))
               .Returns("cX");

            _controller.ModelState.AddModelError("X", "X");
            var vm = new JobFormViewModel();

            var vr = await _controller.AddJob(vm) as ViewResult;
            Assert.That(vr, Is.Not.Null);
            Assert.That(vr.Model, Is.SameAs(vm));
        }

        [Test]
        public async Task AddJob_POST_CreatesOffer_AndRedirects()
        {
            var company = new User
            {
                Id = "c5",
                CompanyProfile = new CompanyProfile
                {
                    CompanyName = "TestCo",
                    ContactEmail = "tc@tc"
                }
            };
            _ctx.Users.Add(company);
            await _ctx.SaveChangesAsync();

            _um.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>()))
               .Returns("c5");

            var vm = new JobFormViewModel
            {
                Title = "T",
                Description = "D",
                Salary = 100,
                CategoryId = 0
            };

            var redirect = await _controller.AddJob(vm) as RedirectToActionResult;
            Assert.That(redirect, Is.Not.Null);
            Assert.That(redirect.ActionName, Is.EqualTo("ManageJobs"));
            Assert.That(_ctx.Offers.Any(o => o.Title == "T"), Is.True);
            Assert.That(_controller.TempData["SuccessMessage"], Is.Not.Null);
        }

        [Test]
        public async Task ApproveApplication_ChangesStatus_AndSendsEmail()
        {
            var company = new User
            {
                Id = "c6",
                CompanyProfile = new CompanyProfile
                {
                    CompanyName = "TestCo",
                    ContactEmail = "hr@test.co"
                }
            };
            var candidate = new User
            {
                Id = "u1",
                Email = "cand@u"
            };
            var offer = new Offer
            {
                OfferId = 1,
                CompanyId = "c6",
                Company = company,
                Title = "My Job",
                Description = "Desc",
                CreatedAt = DateTime.UtcNow
            };
            var application = new JobApplication
            {
                Id = 9,
                OfferId = 1,
                Offer = offer,
                UserId = "u1",
                User = candidate,
                Status = ApplicationStatus.Pending
            };

            _ctx.Users.AddRange(company, candidate);
            _ctx.Offers.Add(offer);
            _ctx.JobApplications.Add(application);
            await _ctx.SaveChangesAsync();

            _um.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>()))
               .Returns("c6");

            var result = await _controller.ApproveApplication(9) as RedirectToActionResult;
            Assert.That(_ctx.JobApplications.Find(9).Status,
                        Is.EqualTo(ApplicationStatus.Approved));

            _email.Verify(e => e.SendEmailAsync(
                "cand@u",
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Once);

            Assert.That(result.ActionName, Is.EqualTo("ViewApplications"));
        }

        [Test]
        public async Task RejectApplication_ChangesStatus_AndSendsEmail()
        {
            _um.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>()))
               .Returns("c7");

            var user = new User
            {
                Id = "u2",
                Email = "to@t"
            };
            var company = new User
            {
                Id = "c7",
                CompanyProfile = new CompanyProfile
                {
                    CompanyName = "TestCo",
                    ContactEmail = "hr@hr"
                }
            };
            var offer = new Offer
            {
                OfferId = 2,
                Company = company,
                CompanyId = "c7",
                Title = "J",
                Description = "D",
                CreatedAt = DateTime.UtcNow
            };
            var app = new JobApplication
            {
                Id = 10,
                Offer = offer,
                OfferId = 2,
                User = user,
                UserId = "u2",
                Status = ApplicationStatus.Pending
            };

            _ctx.Users.AddRange(user, company);
            _ctx.Offers.Add(offer);
            _ctx.JobApplications.Add(app);
            await _ctx.SaveChangesAsync();

            var res = await _controller.RejectApplication(10) as RedirectToActionResult;
            Assert.That(_ctx.JobApplications.Find(10).Status,
                        Is.EqualTo(ApplicationStatus.Rejected));

            _email.Verify(e => e.SendEmailAsync(
                "to@t",
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Once);

            Assert.That(res.ActionName, Is.EqualTo("ViewApplications"));
        }
    }
}
