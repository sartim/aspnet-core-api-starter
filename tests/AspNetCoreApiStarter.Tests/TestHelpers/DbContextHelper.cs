using Microsoft.EntityFrameworkCore;
using AspNetCoreApiStarter.Data;

namespace AspNetCoreApiStarter.Tests.TestHelpers
{
    public static class DbContextHelper
    {
        public static ApplicationDbContext GetInMemoryDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new ApplicationDbContext(options);
        }
    }
}
