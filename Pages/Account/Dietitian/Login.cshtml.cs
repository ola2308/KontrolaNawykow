using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using KontrolaNawykow.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace KontrolaNawyków.Pages.Account.Dietitian
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public LoginModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty, Required(ErrorMessage = "Nazwa u¿ytkownika jest wymagana")]
        public string Username { get; set; }

        [BindProperty, Required(ErrorMessage = "Has³o jest wymagane"), DataType(DataType.Password)]
        public string Password { get; set; }

        public string ErrorMessage { get; set; }

        public void OnGet()
        {
            // tutaj ewentualnie TempData na sukces rejestracji dietetyka
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            // LOGIKA LOGOWANIA (do dostosowania, np. sprawdzenie roli “Dietitian”)
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Username == Username);
            if (user == null || !BCrypt.Net.BCrypt.Verify(Password, user.PasswordHash))
            {
                ErrorMessage = "Nieprawid³owa nazwa u¿ytkownika lub has³o.";
                return Page();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email)
                // tutaj mo¿esz dodaæ claim o roli dietetyka
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7) });

            return RedirectToPage("/Diet/Index"); // panel dietetyka
        }
    }
}
