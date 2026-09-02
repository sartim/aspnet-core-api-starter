using Microsoft.EntityFrameworkCore;
using AspNetCoreApiStarter.Data;

namespace AspNetCoreApiStarter.Tests.TestHelpers
{
    public static class DbContextHelper
    {
        public static ShopDbContext GetInMemoryDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ShopDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new ShopDbContext(options);
        }
    }
}
