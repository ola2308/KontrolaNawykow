using KontrolaNawykow;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using KontrolaNawykow.Models;
using Microsoft.AspNetCore.Hosting;
using System.Linq;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDb");
            });

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();

            // Dodaj testowego użytkownika
            db.Users.Add(new User
            {
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test123!")
            });
            db.SaveChanges();
        });
    }
}
