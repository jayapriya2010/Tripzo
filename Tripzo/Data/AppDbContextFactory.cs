using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Tripzo.Data
{
    /// <summary>
    /// Factory for creating AppDbContext at design time (for EF Core migrations).
    /// This is used by 'dotnet ef' and Package Manager Console commands.
    /// </summary>
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

            // Use the same connection string as in appsettings.json
            var connectionString = "Server=JANANI\\SQLEXPRESS;Database=Tripzo;Trusted_Connection=True;TrustServerCertificate=True;";

            optionsBuilder.UseSqlServer(connectionString);

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
