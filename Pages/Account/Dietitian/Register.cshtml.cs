using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using KontrolaNawykow.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace KontrolaNawyk�w.Pages.Account.Dietitian
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public RegisterModel(ApplicationDbContext context) => _context = context;

        [BindProperty, Required(ErrorMessage = "Nazwa u�ytkownika jest wymagana")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "3�50 znak�w")]
        public string Username { get; set; }

        [BindProperty, Required(ErrorMessage = "Email jest wymagany")]
        [EmailAddress(ErrorMessage = "Nieprawid�owy email")]
        public string Email { get; set; }

        [BindProperty, Required(ErrorMessage = "Has�o jest wymagane")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Min. 6 znak�w")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [BindProperty, Required, Compare("Password", ErrorMessage = "Has�a musz� by� takie same")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }

        public string ErrorMessage { get; set; }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            if (await _context.Users.AnyAsync(u => u.Username == Username))
            {
                ErrorMessage = "Nazwa u�ytkownika ju� istnieje.";
                return Page();
            }
            if (await _context.Users.AnyAsync(u => u.Email == Email))
            {
                ErrorMessage = "Email ju� u�ywany.";
                return Page();
            }

            var user = new User
            {
                Username = Username,
                Email = Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password),
                CreatedAt = DateTime.UtcNow
                // tutaj mo�esz doda� Role = "Dietitian"
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["RegisterSuccess"] = "Zarejestrowano dietetyka pomy�lnie!";
            return RedirectToPage("/Account/Dietitian/Login");
        }
    }
}
