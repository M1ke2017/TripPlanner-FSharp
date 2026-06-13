namespace TripPlanner.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TripPlanner.Persistence;
public class DesignTimeFactory : IDesignTimeDbContextFactory<TripContext>
{
    public TripContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TripContext>();

        var conntectionString = "Host=localhost;Database=travelplannerdb;Username=postgres;Password=postgres12";

        var migrationsAssemblyName = typeof(DesignTimeFactory).Assembly.GetName().Name;

        optionsBuilder.UseNpgsql(
            conntectionString,
            npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly("TripPlanner.Migrations");
            });

        return new TripContext(optionsBuilder.Options);
    }
}
