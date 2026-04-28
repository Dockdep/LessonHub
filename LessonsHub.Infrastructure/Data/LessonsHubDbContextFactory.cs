using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace LessonsHub.Infrastructure.Data;

public class LessonsHubDbContextFactory : IDesignTimeDbContextFactory<LessonsHubDbContext>
{
    public LessonsHubDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LessonsHubDbContext>();
        
        // We just need a dummy connection string to generate the migration. 
        // We're not applying it yet.
        optionsBuilder.UseNpgsql("Host=localhost;Database=LessonsHub;Username=postgres;Password=password123");

        return new LessonsHubDbContext(optionsBuilder.Options);
    }
}
