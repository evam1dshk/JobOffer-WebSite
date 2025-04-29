using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using JobListingSite.Data.Entities;
using JobListingSite.Data.Enums;
using JobListingSite.Web.Controllers;
using JobListingSite.Web.Data;
using JobListingSite.Web.Models.Company;
using JobListingSite.Web.Models.HR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using X.PagedList;

namespace JobListingSite.Test.Controllers
{
    [TestFixture]
    public class HRControllerTests
    {
        private HRController _controller;
        private JobListingDbContext _ctx;
        private Mock<UserManager<User>> _um;

        [SetUp]
        public void SetUp()
        {
            var opts = new DbContextOptionsBuilder<JobListingDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _ctx = new JobListingDbContext(opts);

            var store = new Mock<IUserStore<User>>();
            _um = new Mock<UserManager<User>>(
                store.Object, null, null, null, null, null, null, null, null);

            _controller = new HRController(_ctx, _um.Object)
            {
                // TempData is used in some actions
                TempData = new TempDataDictionary(
                    new DefaultHttpContext(),
                    Mock.Of<ITempDataProvider>())
            };
        }

        [Test]
        public async Task Dashboard_ReturnsViewWithCorrectModel()
        {
            // seed one category
            _ctx.Categories.Add(new Category { CategoryId = 1, Name = "IT" });

            // seed three offers with all required fields
            for (int i = 1; i <= 3; i++)
            {
                _ctx.Offers.Add(new Offer
                {
                    OfferId = i,
                    Title = $"Job {i}",
                    Description = "Whatever",
                    CreatedAt = DateTime.UtcNow.AddDays(-i),
                    CategoryId = 1
                });
            }
            // seed some applications for totals
            _ctx.JobApplications.Add(new JobApplication { Id = 1, OfferId = 1, UserId = "u1", Status = ApplicationStatus.Pending });
            _ctx.JobApplications.Add(new JobApplication { Id = 2, OfferId = 1, UserId = "u2", Status = ApplicationStatus.Approved });
            _ctx.JobApplications.Add(new JobApplication { Id = 3, OfferId = 2, UserId = "u3", Status = ApplicationStatus.Rejected });
            await _ctx.SaveChangesAsync();

            var result = await _controller.Dashboard(null, null, 1) as ViewResult;
            Assert.That(result, Is.Not.Null);

            var vm = result!.Model as HRDashboardViewModel;
            Assert.That(vm!.TotalJobs, Is.EqualTo(3));
            Assert.That(vm.TotalApplications, Is.EqualTo(3));
            Assert.That(vm.PendingApplications, Is.EqualTo(1));
            Assert.That(vm.ApprovedApplications, Is.EqualTo(1));
            Assert.That(vm.RejectedApplications, Is.EqualTo(1));
            Assert.That(vm.RecentOffers, Is.InstanceOf<IPagedList<Offer>>());
            Assert.That(vm.AllCategories, Has.One.Items);
        }

        [Test]
        public void ViewApplications_ReturnsNotFound_IfOfferMissing()
        {
            var result = _controller.ViewApplications(999, null, 1);
            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task ViewApplications_ReturnsView_WithAllApplications()
        {
            // 1) seed an offer (fill all required non-nullable properties)
            var offer = new Offer
            {
                OfferId = 10,
                Title = "X",
                Description = "D",
                CreatedAt = DateTime.UtcNow
            };
            _ctx.Offers.Add(offer);

            // 2) seed users so the Include(a=>a.User) will actually load them
            var u1 = new User { Id = "u1", Name = "User One", Profile = null! };
            var u2 = new User { Id = "u2", Name = "User Two", Profile = null! };
            _ctx.Users.AddRange(u1, u2);

            // 3) seed two applications
            _ctx.JobApplications.AddRange(new[] {
                new JobApplication { Id = 100, OfferId = 10, UserId = "u1", User = u1, Status = ApplicationStatus.Pending },
                new JobApplication { Id = 101, OfferId = 10, UserId = "u2", User = u2, Status = ApplicationStatus.Approved }
            });

            await _ctx.SaveChangesAsync();

            var result = _controller.ViewApplications(10, null, 1) as ViewResult;
            Assert.That(result, Is.Not.Null);

            var vm = result!.Model as JobApplicationsViewModel;
            var list = vm!.ApplicationsPaged;
            Assert.That(list.TotalItemCount, Is.EqualTo(2), "Should see both applications when no filter is applied");
        }

        [Test]
        public async Task ViewApplications_ReturnsView_WithFilteredApplications()
        {
            // same seeding for offer and users
            var offer = new Offer
            {
                OfferId = 20,
                Title = "Y",
                Description = "Desc",
                CreatedAt = DateTime.UtcNow
            };
            _ctx.Offers.Add(offer);
            var u1 = new User { Id = "u1", Name = "User A", Profile = null! };
            var u2 = new User { Id = "u2", Name = "User B", Profile = null! };
            _ctx.Users.AddRange(u1, u2);

            _ctx.JobApplications.AddRange(new[] {
                new JobApplication { Id = 200, OfferId = 20, UserId = "u1", User = u1, Status = ApplicationStatus.Pending },
                new JobApplication { Id = 201, OfferId = 20, UserId = "u2", User = u2, Status = ApplicationStatus.Approved }
            });

            await _ctx.SaveChangesAsync();

            // filter only Approved
            var result = _controller.ViewApplications(20, ApplicationStatus.Approved.ToString(), 1) as ViewResult;
            Assert.That(result, Is.Not.Null);

            var vm = result!.Model as JobApplicationsViewModel;
            var list = vm!.ApplicationsPaged;
            Assert.That(list.TotalItemCount, Is.EqualTo(1), "Only the approved application should survive the filter");
            Assert.That(list.Single().Status, Is.EqualTo(ApplicationStatus.Approved));
        }

        [Test]
        public async Task ApproveApplication_SetsStatus_AndRedirects()
        {
            _ctx.JobApplications.Add(new JobApplication
            {
                Id = 42,
                OfferId = 100,
                UserId = "some-user",
                Status = ApplicationStatus.Pending
            });
            await _ctx.SaveChangesAsync();

            var res = await _controller.ApproveApplication(42) as RedirectToActionResult;
            Assert.That(res!.ActionName, Is.EqualTo("ViewApplications"));

            var updated = await _ctx.JobApplications.FindAsync(42);
            Assert.That(updated!.Status, Is.EqualTo(ApplicationStatus.Approved));
            Assert.That(_controller.TempData["Success"], Is.EqualTo("Application approved."));
        }

        [Test]
        public async Task RejectApplication_SetsStatus_AndRedirects()
        {
            _ctx.JobApplications.Add(new JobApplication
            {
                Id = 43,
                OfferId = 101,
                UserId = "u1",
                Status = ApplicationStatus.Pending
            });
            await _ctx.SaveChangesAsync();

            var res = await _controller.RejectApplication(43) as RedirectToActionResult;
            Assert.That(res!.ActionName, Is.EqualTo("ViewApplications"));

            var updated = await _ctx.JobApplications.FindAsync(43);
            Assert.That(updated!.Status, Is.EqualTo(ApplicationStatus.Rejected));
            Assert.That(_controller.TempData["Warning"], Is.EqualTo("Application rejected."));
        }

        [Test]
        public async Task DeleteJob_RemovesAndRedirects()
        {
            _ctx.Offers.Add(new Offer
            {
                OfferId = 7,
                Title = "T",
                Description = "D",
                CreatedAt = DateTime.UtcNow
            });
            await _ctx.SaveChangesAsync();

            var res = await _controller.DeleteJob(7) as RedirectToActionResult;
            Assert.That(res!.ActionName, Is.EqualTo("Dashboard"));
            Assert.That(await _ctx.Offers.FindAsync(7), Is.Null);
            Assert.That(_controller.TempData["SuccessMessage"], Is.EqualTo("Job offer deleted successfully!"));
        }

        [Test]
        public void CreateTicket_GET_ReturnsView()
        {
            var res = _controller.CreateTicket() as ViewResult;
            Assert.That(res, Is.Not.Null);
        }

        [Test]
        public async Task CreateTicket_POST_SavesAndRedirects()
        {
            var fakeUser = new User { Id = "hr123" };
            _um.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
               .ReturnsAsync(fakeUser);

            var vm = new CreateTicketViewModel
            {
                Title = "Help",
                Description = "Broken",
                Priority = TicketPriority.High
            };

            var res = await _controller.CreateTicket(vm) as RedirectToActionResult;
            Assert.That(res!.ActionName, Is.EqualTo("CreatedTicket"));

            var saved = await _ctx.HRTickets.FirstOrDefaultAsync();
            Assert.That(saved!.Title, Is.EqualTo("Help"));
            Assert.That(saved.CreatedById, Is.EqualTo("hr123"));
            Assert.That(_controller.TempData["TicketCreated"], Is.EqualTo(true));
            Assert.That(_controller.TempData["NewTicketId"], Is.EqualTo(saved.Id));
        }

        [Test]
        public async Task CreatedTicket_ReturnsViewWithTickets()
        {
            var fakeUser = new User { Id = "hrX" };
            _um.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
               .ReturnsAsync(fakeUser);

            _ctx.HRTickets.AddRange(new[] {
                new HRTicket { Id = 1, Title = "A", Description = "D", CreatedById = "hrX" },
                new HRTicket { Id = 2, Title = "B", Description = "D", CreatedById = "hrX" }
            });
            await _ctx.SaveChangesAsync();

            var res = await _controller.CreatedTicket(page: 1) as ViewResult;
            var list = res!.Model as IPagedList<HRTicket>;
            Assert.That(list!.TotalItemCount, Is.EqualTo(2));
        }

        [Test]
        public async Task DeleteTicket_RemovesAndRedirects()
        {
            _ctx.HRTickets.Add(new HRTicket { Id = 5, Title = "X", Description = "Y", CreatedById = "u1" });
            await _ctx.SaveChangesAsync();

            var res = await _controller.DeleteTicket(5) as RedirectToActionResult;
            Assert.That(res!.ActionName, Is.EqualTo("CreatedTicket"));
            Assert.That(await _ctx.HRTickets.FindAsync(5), Is.Null);
            Assert.That(_controller.TempData["SuccessMessage"], Is.EqualTo("Ticket deleted successfully!"));
        }
    }
}
