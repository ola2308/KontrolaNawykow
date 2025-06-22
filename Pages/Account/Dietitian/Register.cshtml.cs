using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;
using KontrolaNawykow.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace KontrolaNawyków.Pages.Account.Dietitian
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public RegisterModel(ApplicationDbContext context) => _context = context;

        [BindProperty, Required(ErrorMessage = "Imiê jest wymagane")]
        public string Imie { get; set; }

        [BindProperty, Required(ErrorMessage = "Nazwisko jest wymagane")]
        public string Nazwisko { get; set; }

        [BindProperty, Required(ErrorMessage = "Email jest wymagany")]
        [EmailAddress(ErrorMessage = "Nieprawid³owy email")]
        public string Email { get; set; }

        [BindProperty]
        public IFormFile Zdjecie { get; set; }

        [BindProperty, Required(ErrorMessage = "Specjalizacja jest wymagana")]
        public string Specjalizacja { get; set; }

        [BindProperty, Required(ErrorMessage = "Telefon jest wymagany")]
        public string Telefon { get; set; }

        [BindProperty, Required(ErrorMessage = "Has³o jest wymagane")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Min. 6 znaków")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [BindProperty, Required, Compare("Password", ErrorMessage = "Has³a musz¹ byæ takie same")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }

        public string ErrorMessage { get; set; }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            if (await _context.Dietetycy.AnyAsync(d => d.Email == Email))
            {
                ErrorMessage = "Email ju¿ u¿ywany.";
                return Page();
            }

            byte[] zdjData = null;
            if (Zdjecie != null && Zdjecie.Length > 0)
            {
                using var ms = new MemoryStream();
                await Zdjecie.CopyToAsync(ms);
                zdjData = ms.ToArray();
            }

            var dietetyk = new Dietetyk
            {
                Imie = Imie,
                Nazwisko = Nazwisko,
                Email = Email,
                Zdjecie = zdjData,
                Specjalizacja = Specjalizacja,
                Telefon = Telefon,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password)
            };

            _context.Dietetycy.Add(dietetyk);
            await _context.SaveChangesAsync();

            TempData["RegisterSuccess"] = "Zarejestrowano dietetyka pomyœlnie!";
            return RedirectToPage("/Account/Dietitian/Login");
        }
    }
}
