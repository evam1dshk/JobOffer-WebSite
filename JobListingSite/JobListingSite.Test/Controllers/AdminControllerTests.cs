using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;
using JobListingSite.Data.Entities;
using JobListingSite.Data.Enums;
using JobListingSite.Web.Controllers;
using JobListingSite.Web.Data;
using JobListingSite.Web.Models.Admin;
using JobListingSite.Web.Models.JobListing;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using NUnit.Framework;
using X.PagedList;

namespace JobListingSite.Test.Controllers
{
    [TestFixture]
    public class AdminControllerTests
    {
        private AdminController _controller;
        private JobListingDbContext _context;
        private Mock<UserManager<User>> _userManagerMock;
        private Mock<RoleManager<IdentityRole>> _roleManagerMock;

        [SetUp]
        public void Setup()
        {
            // 1) In‐memory EF
            var opts = new DbContextOptionsBuilder<JobListingDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new JobListingDbContext(opts);

            // 2) UserManager mock
            var userStore = new Mock<IUserStore<User>>();
            _userManagerMock = new Mock<UserManager<User>>(
                userStore.Object, null, null, null, null, null, null, null, null
            );

            // 3) RoleManager mock + EF-compatible async Roles provider
            var roleStore = new Mock<IRoleStore<IdentityRole>>();
            _roleManagerMock = new Mock<RoleManager<IdentityRole>>(
                roleStore.Object,
                new IRoleValidator<IdentityRole>[] { new RoleValidator<IdentityRole>() },
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                null
            );

            // seed four roles, wrap in TestAsyncEnumerable so ToListAsync works
            var rolesData = new[]
            {
                new IdentityRole("Admin"),
                new IdentityRole("Registered"),
                new IdentityRole("Company"),
                new IdentityRole("HR")
            };
            var asyncRoles = new TestAsyncEnumerable<IdentityRole>(rolesData);
            _roleManagerMock.Setup(r => r.Roles).Returns(asyncRoles);

            // 4) Build controller
            _controller = new AdminController(
                _context,
                _userManagerMock.Object,
                _roleManagerMock.Object
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

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }



        [Test]
        public async Task Dashboard_PopulatesViewBag_AndReturnsView()
        {
            // seed data
            _context.Users.Add(new User { Id = "u1", IsCompany = false });
            _context.Users.Add(new User { Id = "c1", IsCompany = true });
            _context.Categories.Add(new Data.Entities.Category { CategoryId = 1, Name = "Tech" });
            _context.Offers.Add(new Offer
            {
                OfferId = 1,
                CreatedAt = DateTime.UtcNow,
                CategoryId = 1,
                Title = "T",
                Description = "D"
            });
            await _context.SaveChangesAsync();

            var result = await _controller.Dashboard() as ViewResult;
            Assert.That(result, Is.Not.Null);

            // now expect 2 users total
            Assert.That(_controller.ViewBag.TotalUsers, Is.EqualTo(2));
            Assert.That(_controller.ViewBag.TotalCompanies, Is.EqualTo(1));
            Assert.That(_controller.ViewBag.TotalJobs, Is.EqualTo(1));
            Assert.That(_controller.ViewBag.TotalCategories, Is.EqualTo(1));

            // NewOffersByDay comes back as an anonymous‐type list
            var chartData = _controller.ViewBag.NewOffersByDay as IEnumerable;
            Assert.That(chartData, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public async Task ManageCategories_ShowsAll()
        {
            _context.Categories.AddRange(
                new Data.Entities.Category { Name = "X" },
                new Data.Entities.Category { Name = "Y" }
            );
            await _context.SaveChangesAsync();

            var vr = await _controller.ManageCategories() as ViewResult;
            var list = vr!.Model as List<Data.Entities.Category>;
            Assert.That(list!.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task AddCategory_Post_Valid_RedirectsAndSaves()
        {
            var cat = new Data.Entities.Category { Name = "New" };
            var redirect = await _controller.AddCategory(cat) as RedirectToActionResult;

            Assert.That(redirect!.ActionName, Is.EqualTo("ManageCategories"));
            Assert.That(_context.Categories.Any(c => c.Name == "New"), Is.True);
            Assert.That(_controller.TempData["SuccessMessage"], Is.EqualTo("Category added successfully!"));
        }

        [Test]
        public async Task AddCategory_Post_Invalid_ReturnsView()
        {
            _controller.ModelState.AddModelError("X", "err");
            var cat = new Data.Entities.Category();
            var vr = await _controller.AddCategory(cat) as ViewResult;

            Assert.That(vr, Is.Not.Null);
            Assert.That(vr!.Model, Is.SameAs(cat));
        }

        [Test]
        public async Task EditCategory_GetAndPost_Works()
        {
            // Arrange: seed an existing category
            var cat = new Category { CategoryId = 5, Name = "Old" };
            _context.Categories.Add(cat);
            await _context.SaveChangesAsync();

            // Act (GET)
            var getResult = await _controller.EditCategory(5) as ViewResult;
            Assert.That(getResult, Is.Not.Null);
            var getModel = (Category)getResult!.Model;
            Assert.That(getModel.Name, Is.EqualTo("Old"));

            // Detach the GET‐loaded entity so EF Core won't track it when we POST a new instance
            _context.Entry(getModel).State = EntityState.Detached;

            // Act (POST)
            var updated = new Category { CategoryId = 5, Name = "New" };
            var postResult = await _controller.EditCategory(updated) as RedirectToActionResult;

            // Assert
            Assert.That(postResult, Is.Not.Null);
            Assert.That(postResult!.ActionName, Is.EqualTo("ManageCategories"));
            var dbCat = await _context.Categories.FindAsync(5);
            Assert.That(dbCat!.Name, Is.EqualTo("New"));
        }

        [Test]
        public async Task DeleteCategory_RemovesAndRedirects()
        {
            var cat = new Data.Entities.Category { CategoryId = 7, Name = "ToDel" };
            _context.Categories.Add(cat);
            await _context.SaveChangesAsync();

            var redirect = await _controller.DeleteCategory(7) as RedirectToActionResult;
            Assert.That(redirect!.ActionName, Is.EqualTo("ManageCategories"));
            Assert.That(_context.Categories.Find(7), Is.Null);
            Assert.That(_controller.TempData["SuccessMessage"], Is.EqualTo("Category deleted successfully!"));
        }

        [Test]
        public async Task ManageCompanies_ReturnsPagedList()
        {
            var u1 = new User { Id = "c1", IsCompany = true, CompanyProfile = new CompanyProfile { CompanyName = "A" } };
            var u2 = new User { Id = "c2", IsCompany = true, CompanyProfile = new CompanyProfile { CompanyName = "B" } };
            _context.Users.AddRange(u1, u2);
            await _context.SaveChangesAsync();

            var vr = await _controller.ManageCompanies(page: 1) as ViewResult;
            var page = vr!.Model as IPagedList<UserViewModel>;
            Assert.That(page!.TotalItemCount, Is.EqualTo(2));
        }

        [Test]
        public async Task PromoteDemoteLockUnlock_UserFlows()
        {
            var user = new User { Id = "u1", Email = "user@example.com", UserName = "user1" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _userManagerMock.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Registered" });
            _userManagerMock.Setup(m => m.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(m => m.AddToRoleAsync(user, It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            // Promote
            var promote = await _controller.PromoteUser("u1", "Company") as RedirectToActionResult;
            Assert.That(promote!.ActionName, Is.EqualTo("ManageUsers"));
            Assert.That(_controller.TempData["SuccessMessage"]?.ToString(),
                        Does.Contain("promoted to Company"));

            // Demote
            _userManagerMock.Setup(m => m.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Company" });
            var demote = await _controller.DemoteUser("u1") as RedirectToActionResult;
            Assert.That(demote!.ActionName, Is.EqualTo("ManageUsers"));
            Assert.That(_controller.TempData["SuccessMessage"]?.ToString(),
                        Does.Contain("demoted to Registered"));

            // Lock
            var lockResult = await _controller.LockUser("u1") as RedirectToActionResult;
            Assert.That(lockResult!.ActionName, Is.EqualTo("ManageUsers"));
            Assert.That(_controller.TempData["SuccessMessage"]?.ToString(),
                        Does.Contain("locked successfully"));

            // Unlock
            var unlockResult = await _controller.UnlockUser("u1") as RedirectToActionResult;
            Assert.That(unlockResult!.ActionName, Is.EqualTo("ManageUsers"));
            Assert.That(_controller.TempData["SuccessMessage"]?.ToString(),
                        Does.Contain("unlocked successfully"));
        }

        [Test]
        public void CreateCategory_Get_ReturnsView()
        {
            var vr = _controller.CreateCategory() as ViewResult;
            Assert.That(vr, Is.Not.Null);
        }

        [Test]
        public async Task CreateCategory_Post_RedirectsOnValid()
        {
            var cat = new Data.Entities.Category { Name = "Z" };
            var r = await _controller.CreateCategory(cat) as RedirectToActionResult;
            Assert.That(r!.ActionName, Is.EqualTo("ManageCategories"));
            Assert.That(_controller.TempData["SuccessMessage"], Is.EqualTo("Category created successfully!"));
        }

        [Test]
        public async Task ChangeRole_GetAndPost_Scenarios()
        {
            // seed user
            var user = new User { Id = "u1", UserName = "bob", Email = "bob@example.com" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _userManagerMock.Setup(u => u.FindByIdAsync("u1")).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Registered" });

            // GET
            var getResult = await _controller.ChangeRole("u1") as ViewResult;
            var getModel = getResult!.Model as ChangeRoleViewModel;
            Assert.That(getModel!.AvailableRoles, Contains.Item("Admin"));
            Assert.That(getModel.AvailableRoles, Contains.Item("Registered"));

            // POST
            getModel.NewRole = "Admin";
            _userManagerMock.Setup(u => u.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(u => u.AddToRoleAsync(user, "Admin"))
                .ReturnsAsync(IdentityResult.Success);

            var postResult = await _controller.ChangeRole(getModel) as RedirectToActionResult;
            Assert.That(postResult!.ActionName, Is.EqualTo(nameof(AdminController.ManageUsers)));
            Assert.That(_controller.TempData["SuccessMessage"]?.ToString()!,
                        Does.Contain("Role changed to Admin successfully!"));
        }

        [Test]
        public async Task ViewCompaniesAndApproveDeleteCompany()
        {
            var user = new User
            {
                Id = "u1",
                UserName = "company1",
                Email = "hr@company.com",
                IsCompany = true,
                CompanyProfile = new CompanyProfile
                {
                    Id = 9,
                    CompanyName = "C1",
                    ContactEmail = "hr@company.com"
                }
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var viewResult = await _controller.ViewCompanies() as ViewResult;
            Assert.That(viewResult, Is.Not.Null);

            var list = (List<CompanyProfile>)viewResult!.Model!;
            Assert.That(list.Count, Is.EqualTo(1), "should return exactly the one seeded profile");

            var approveResult = await _controller.ApproveCompany(9) as RedirectToActionResult;
            Assert.That(approveResult, Is.Not.Null);
            Assert.That(approveResult!.ActionName, Is.EqualTo("ViewCompanies"));
            Assert.That(_context.CompanyProfiles.Find(9)!.IsVerified, Is.True);

            var deleteResult = await _controller.DeleteCompany(9) as RedirectToActionResult;
            Assert.That(deleteResult, Is.Not.Null);
            Assert.That(deleteResult!.ActionName, Is.EqualTo("ViewCompanies"));
            Assert.That(_context.CompanyProfiles.Find(9), Is.Null, "profile should have been removed");
        }

        [Test]
        public async Task ViewCompanyProfile_ReturnsOrRedirects()
        {
            var user = new User
            {
                Id = "u2",
                UserName = "companyUser",
                Email = "hr@company.com",
                IsCompany = true
            };
            _context.Users.Add(user);

            var cp = new CompanyProfile
            {
                Id = 20,
                CompanyName = "X Corp",
                ContactEmail = "hr@company.com",
                UserId = "u2"
            };
            _context.CompanyProfiles.Add(cp);

            await _context.SaveChangesAsync();

            var okResult = await _controller.ViewCompanyProfile(20) as ViewResult;
            Assert.That(okResult, Is.Not.Null, "Should return the View for an existing profile");

            var model = okResult!.Model as CompanyProfile;
            Assert.That(model, Is.Not.Null);
            Assert.That(model!.CompanyName, Is.EqualTo("X Corp"));

            var redirectResult = await _controller.ViewCompanyProfile(999) as RedirectToActionResult;
            Assert.That(redirectResult, Is.Not.Null, "Should redirect when profile not found");
            Assert.That(redirectResult!.ActionName, Is.EqualTo(nameof(_controller.ViewCompanies)));
        }


        [Test]
        public async Task ManageUsers_And_ViewOffers_Paginate()
        {
            _context.Users.AddRange(
                new User { Id = "u1", IsCompany = false },
                new User { Id = "u2", IsCompany = false }
            );
            _context.Categories.Add(new Data.Entities.Category { CategoryId = 1, Name = "C" });
            _context.Offers.Add(new Offer
            {
                OfferId = 1,
                Title = "T",
                Description = "D",
                CreatedAt = DateTime.UtcNow,
                CategoryId = 1
            });
            await _context.SaveChangesAsync();

            _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<User>()))
                .ReturnsAsync(new List<string>());

            var mu = await _controller.ManageUsers(1) as ViewResult;
            Assert.That(((IPagedList<UserViewModel>)mu!.Model).TotalItemCount, Is.EqualTo(2));

            var vo = await _controller.ViewOffers(1) as ViewResult;
            Assert.That(((IPagedList<Offer>)vo!.Model).TotalItemCount, Is.EqualTo(1));
        }

        [Test]
        public async Task EditOffer_GetAndPost_Works()
        {
            _context.Categories.Add(new Data.Entities.Category { CategoryId = 1, Name = "Cat" });
            _context.Offers.Add(new Offer
            {
                OfferId = 33,
                Title = "T",
                Description = "D",
                Salary = 5,
                CreatedAt = DateTime.UtcNow,
                CategoryId = 1
            });
            await _context.SaveChangesAsync();

            var get = await _controller.EditOffer(33) as ViewResult;
            Assert.That(((JobFormViewModel)get!.Model).OfferId, Is.EqualTo(33));

            var vm = new JobFormViewModel
            {
                OfferId = 33,
                Title = "NewT",
                Description = "NewD",
                Salary = 10,
                CategoryId = 1
            };
            var post = await _controller.EditOffer(vm) as RedirectToActionResult;
            Assert.That(post!.ActionName, Is.EqualTo("ViewOffers"));
            Assert.That(_context.Offers.Find(33)!.Title, Is.EqualTo("NewT"));
        }

        [Test]
        public async Task EditCompanyProfile_GetAndPost_Works()
        {
            var user = new User { Id = "u3", UserName = "compuser", Email = "comp@example.com" };
            _context.Users.Add(user);

            var cp = new CompanyProfile
            {
                Id = 55,
                CompanyName = "X",
                UserId = user.Id,
                Description = "Old Desc",
                Industry = "Old Ind",
                ContactEmail = "old@c.com",
                Phone = "000",
                Location = "Old L",
                NumberOfEmployees = 10
            };
            _context.CompanyProfiles.Add(cp);
            await _context.SaveChangesAsync();

            var getResult = await _controller.EditCompanyProfile(55);
            Assert.That(getResult, Is.InstanceOf<ViewResult>());
            var getView = (ViewResult)getResult;

            Assert.That(getView.Model, Is.InstanceOf<CompanyProfile>());
            var getModel = (CompanyProfile)getView.Model;
            Assert.That(getModel.Id, Is.EqualTo(55));
            Assert.That(getModel.CompanyName, Is.EqualTo("X"));

            _context.Entry(getModel).State = EntityState.Detached;

            var updated = new CompanyProfile
            {
                Id = 55,
                CompanyName = "Y",
                UserId = user.Id
            };
            var postResult = await _controller.EditCompanyProfile(updated);

            Assert.That(postResult, Is.InstanceOf<RedirectToActionResult>());
            var redirect = (RedirectToActionResult)postResult;
            Assert.That(redirect.ActionName, Is.EqualTo("ManageCompanies"));

            var dbCp = await _context.CompanyProfiles.FindAsync(55);
            Assert.That(dbCp, Is.Not.Null);
            Assert.That(dbCp.CompanyName, Is.EqualTo("Y"));
        }


        [Test]
        public async Task ViewTickets_ResolveDelete_Works()
        {
            var t1 = new HRTicket
            {
                Id = 101,
                Title = "A",
                Description = "B",
                CreatedById = "u1",
                Status = TicketStatus.Open
            };
            _context.HRTickets.Add(t1);
            await _context.SaveChangesAsync();

            var v = await _controller.ViewTickets(1) as ViewResult;
            Assert.That(((IPagedList<HRTicket>)v!.Model).TotalItemCount, Is.EqualTo(1));

            var r = await _controller.ResolveTicket(101) as RedirectToActionResult;
            Assert.That(r!.ActionName, Is.EqualTo("ViewTickets"));
            Assert.That(_context.HRTickets.Find(101)!.Status, Is.EqualTo(TicketStatus.Resolved));

            var d = await _controller.DeleteTicketAdmin(101) as RedirectToActionResult;
            Assert.That(d!.ActionName, Is.EqualTo("ViewTickets"));
            Assert.That(_context.HRTickets.Find(101), Is.Null);
        }

        private class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
        {
            private readonly IQueryProvider _inner;
            public TestAsyncQueryProvider(IQueryProvider inner) => _inner = inner;

            public IQueryable CreateQuery(Expression expression)
                => new TestAsyncEnumerable<TEntity>(expression);

            public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
                => new TestAsyncEnumerable<TElement>(expression);

            public object Execute(Expression expression)
                => _inner.Execute(expression);

            public TResult Execute<TResult>(Expression expression)
                => _inner.Execute<TResult>(expression);

            public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
                => Execute<TResult>(expression);
        }

        private class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
        {
            public TestAsyncEnumerable(IEnumerable<T> enumerable)
                : base(enumerable) { }

            public TestAsyncEnumerable(Expression expression)
                : base(expression) { }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
                => new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());

            IQueryProvider IQueryable.Provider
                => new TestAsyncQueryProvider<T>(this);
        }

        private class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
        {
            private readonly IEnumerator<T> _inner;
            public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;
            public T Current => _inner.Current;
            public ValueTask DisposeAsync() { _inner.Dispose(); return default; }
            public ValueTask<bool> MoveNextAsync() => new ValueTask<bool>(_inner.MoveNext());
        }
    }
}
