using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;
using KontrolaNawykow.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace KontrolaNawyk闚.Pages.Account.Dietitian
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public LoginModel(ApplicationDbContext context) => _context = context;

        [BindProperty, Required(ErrorMessage = "Email jest wymagany")]
        [EmailAddress(ErrorMessage = "Nieprawid這wy email")]
        public string Email { get; set; }

        [BindProperty, Required(ErrorMessage = "Has這 jest wymagane")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public string ErrorMessage { get; set; }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var dietetyk = await _context.Dietetycy
                .SingleOrDefaultAsync(d => d.Email == Email);

            if (dietetyk == null ||
                !BCrypt.Net.BCrypt.Verify(Password, dietetyk.PasswordHash))
            {
                ErrorMessage = "Nieprawid這wy email lub has這.";
                return Page();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, dietetyk.Id.ToString()),
                new Claim(ClaimTypes.Email,           dietetyk.Email),
                new Claim(ClaimTypes.Name,            $"{dietetyk.Imie} {dietetyk.Nazwisko}"),
                new Claim(ClaimTypes.Role,            "Dietitian")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                });

            return RedirectToPage("/Diet/Index");
        }
    }
}
