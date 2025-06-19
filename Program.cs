using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using System;
using KontrolaNawykow.Models;

var builder = WebApplication.CreateBuilder(args);

// Dodanie kontekstu bazy danych SQLite
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new Exception("Brak ConnectionString w pliku konfiguracji!");
    }
    Console.WriteLine($"Połączono z bazą danych SQLite: {connectionString}");
    options.UseSqlite(connectionString);
});

// Konfiguracja CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost",
        policy =>
        {
            policy.WithOrigins("https://localhost:7169", "http://localhost:7169")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});

// Konfiguracja uwierzytelniania cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "KontrolaNawykowAuth";
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

// Konfiguracja sesji
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// WAŻNE: Dodanie MVC i Razor Pages w odpowiedniej kolejności
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddRazorPages(options =>
{
    // Konfiguracja Razor Pages
    options.Conventions.AuthorizePage("/Recipes/Add");
    options.Conventions.AuthorizePage("/Diet/Index");
    options.Conventions.AuthorizePage("/Profile/Index");
    options.Conventions.AuthorizePage("/Profile/Setup");
});

var app = builder.Build();

// Konfiguracja błędów
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// Middleware do logowania żądań
app.Use(async (context, next) =>
{
    Console.WriteLine($"Request: {context.Request.Method} {context.Request.Path} {context.Request.QueryString}");

    if (context.Request.Path.StartsWithSegments("/api"))
    {
        Console.WriteLine($"API Request: {context.Request.Method} {context.Request.Path}");
        Console.WriteLine($"Content-Type: {context.Request.ContentType}");
        Console.WriteLine($"User authenticated: {context.User?.Identity?.IsAuthenticated}");
    }

    await next();

    Console.WriteLine($"Response: {context.Response.StatusCode} for {context.Request.Path}");
});

// Middleware pipeline - KOLEJNOŚĆ JEST WAŻNA!
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("AllowLocalhost");

// Routing musi być przed Authentication i Authorization
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

// WAŻNE: Mapowanie tras - kontrolery API NAJPIERW!
app.MapControllers(); // To obsługuje /api/* endpoints

// Mapowanie tras MVC (dla kontrolerów z widokami)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

// Mapowanie Razor Pages - TO OBSŁUGUJE /Recipes/Add
app.MapRazorPages();

// Dodatkowe routing dla legacy endpoints
app.MapGet("/", async context =>
{
    context.Response.Redirect("/Account/Login");
});

// Custom routing dla często używanych ścieżek
app.MapGet("/Diet", async context =>
{
    context.Response.Redirect("/Diet/Index");
});

app.MapGet("/Recipes", async context =>
{
    context.Response.Redirect("/Recipes/Add");
});

// Debug routing - pokaż wszystkie zarejestrowane trasy
if (app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        if (context.Request.Path == "/debug/routes")
        {
            var endpointDataSource = context.RequestServices.GetRequiredService<EndpointDataSource>();
            var endpoints = endpointDataSource.Endpoints;

            context.Response.ContentType = "text/html";
            await context.Response.WriteAsync("<h1>Registered Routes:</h1><ul>");

            foreach (var endpoint in endpoints)
            {
                var routeEndpoint = endpoint as RouteEndpoint;
                if (routeEndpoint != null)
                {
                    await context.Response.WriteAsync($"<li><strong>{routeEndpoint.RoutePattern}</strong> - {endpoint.DisplayName}</li>");
                }
            }

            await context.Response.WriteAsync("</ul>");
            return;
        }

        await next();
    });
}

Console.WriteLine("=== APLIKACJA STARTUJE ===");
Console.WriteLine("Dostępne endpointy:");
Console.WriteLine("- /Account/Login (Razor Page)");
Console.WriteLine("- /Diet/Index (Razor Page)");
Console.WriteLine("- /Recipes/Add (Razor Page)");
Console.WriteLine("- /api/recipe (API Controller)");
Console.WriteLine("- /api/ingredient (API Controller)");
Console.WriteLine("- /api/mealplan (API Controller)");
Console.WriteLine("- /debug/routes (Debug - lista wszystkich tras)");
Console.WriteLine("=====================================");

app.Run();