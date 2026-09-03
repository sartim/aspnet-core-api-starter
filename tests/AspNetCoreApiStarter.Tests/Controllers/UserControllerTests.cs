using Xunit;
using AspNetCoreApiStarter.Controllers;
using AspNetCoreApiStarter.Data;
using AspNetCoreApiStarter.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace AspNetCoreApiStarter.Tests.Controllers
{
    public class UserControllerTests
    {
        private ApplicationDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task Post_ShouldCreateUser()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var controller = new UserController(dbContext);

            var user = new User
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "johndoe@test.com",
                Phone = 12345678,
                Password = "Strong-password1",
                IsActive = true
            };

            // Act
            var actionResult = await controller.Post(user);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
            var returnedUser = Assert.IsType<User>(createdResult.Value);

            Assert.Equal("John", returnedUser.FirstName);
            Assert.NotNull(returnedUser.Password);
        }

        [Fact]
        public async Task Get_ReturnsPagedAndFilteredResponse()
        {
            await using var dbContext = GetInMemoryDbContext();
            dbContext.Users.AddRange(
                new User { FirstName = "Alice", LastName = "Smith", Email = "alice@test.com", Phone = 1, Password = "Strong-password1", IsActive = true },
                new User { FirstName = "Bob", LastName = "Jones", Email = "bob@test.com", Phone = 2, Password = "Strong-password1", IsActive = true });
            await dbContext.SaveChangesAsync();
            var controller = new UserController(dbContext);

            var actionResult = await controller.Get(new PageQuery { Page = 1, PageSize = 1, Q = "alice" });

            var result = Assert.IsType<OkObjectResult>(actionResult.Result);
            var page = Assert.IsType<PagedResponse<User>>(result.Value);
            Assert.Single(page.Items);
            Assert.Equal("alice@test.com", page.Items[0].Email);
            Assert.Equal(1, page.TotalCount);
        }
    }
}
