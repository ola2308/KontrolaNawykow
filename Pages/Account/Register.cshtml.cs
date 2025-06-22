using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using KontrolaNawykow.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace KontrolaNawykow.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public RegisterModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        [Required(ErrorMessage = "Nazwa uzytkownika jest wymagana")]
        [StringLength(50, ErrorMessage = "Nazwa uzytkownika musi miec od {2} do {1} znakow.", MinimumLength = 3)]
        public string Username { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Email jest wymagany")]
        [EmailAddress(ErrorMessage = "Nieprawidlowy format adresu email")]
        public string Email { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Haslo jest wymagane")]
        [StringLength(100, ErrorMessage = "Haslo musi miec minimum {2} znakow.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Potwierdzenie hasla jest wymagane")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Hasla nie są identyczne")]
        public string ConfirmPassword { get; set; }

        public string ErrorMessage { get; set; }

        public void OnGet()
        {
            Console.WriteLine("✓ Otworzono strone rejestracji.");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                Console.WriteLine("=== ROZPOCZECIE REJESTRACJI ===");

                // Ręcznie pobierz wartości z formularza
                string username = Request.Form["Username"].ToString();
                string email = Request.Form["Email"].ToString();
                string password = Request.Form["Password"].ToString();
                string confirmPassword = Request.Form["ConfirmPassword"].ToString();

                Console.WriteLine($"Recznie pobrane wartosci: Username='{username}', Email='{email}', Password length={password.Length}, ConfirmPassword length={confirmPassword.Length}");

                // Przypisz wartości do właściwości modelu
                Username = username;
                Email = email;
                Password = password;
                ConfirmPassword = confirmPassword;

                // Debugowanie danych formularza
                Console.WriteLine("=== WSZYSTKIE DANE FORMULARZA ===");
                foreach (var key in Request.Form.Keys)
                {
                    if (key.Contains("Password", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"   Klucz: {key}, Dlugosc: {Request.Form[key].ToString().Length}");
                    }
                    else
                    {
                        Console.WriteLine($"   Klucz: {key}, Wartosc: {Request.Form[key]}");
                    }
                }

                // Sprawdzenie ModelState
                if (!ModelState.IsValid)
                {
                    Console.WriteLine("ModelState jest NIEPOPRAWNY!");
                    foreach (var modelError in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        Console.WriteLine($"Blad walidacji: {modelError.ErrorMessage}");
                    }
                    return Page();
                }

                // Sprawdzenie, czy użytkownik istnieje
                if (await _context.Users.AnyAsync(u => u.Username == Username))
                {
                    ErrorMessage = "Uzytkownik o takiej nazwie już istnieje.";
                    Console.WriteLine("❌ Uzytkownik o takiej nazwie już istnieje.");
                    return Page();
                }

                if (await _context.Users.AnyAsync(u => u.Email == Email))
                {
                    ErrorMessage = "Ten adres email jest już uzywany.";
                    Console.WriteLine("Adres email już istnieje w bazie.");
                    return Page();
                }

                // Utworzenie nowego użytkownika
                var user = new User
                {
                    Username = Username,
                    Email = Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password),
                    CreatedAt = DateTime.UtcNow
                };

                // Dodanie użytkownika do bazy danych
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                Console.WriteLine($"Utworzono uzytkownika z ID: {user.Id}");

                // AUTOMATYCZNE LOGOWANIE UŻYTKOWNIKA PO REJESTRACJI
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                Console.WriteLine($"Uzytkownik {user.Username} zostal automatycznie zalogowany.");

                // Zapisanie ID użytkownika w TempData jako backup
                TempData["UserId"] = user.Id;
                TempData.Keep("UserId");

                Console.WriteLine($"Dane zapisane w TempData: UserId={TempData["UserId"]}");
                Console.WriteLine("Przekierowanie do Setup...");

                // Przekierowanie do konfiguracji profilu
                return RedirectToPage("/Profile/Setup");
            }
            catch (Exception ex)
            {
                // Obsługa błędów
                ErrorMessage = "Wystapil blad podczas przetwarzania danych: " + ex.Message;
                Console.WriteLine($"Blad: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return Page();
            }
        }
    }
}