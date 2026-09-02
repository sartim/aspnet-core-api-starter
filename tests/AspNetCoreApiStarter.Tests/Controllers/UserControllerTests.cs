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
                Password = "password123",
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
    }
}
