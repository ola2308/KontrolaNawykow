using KontrolaNawykow.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace KontrolaNawykow.Pages.Admin
{
    public class BansModel : PageModel
    {
        public User CurrentUser { get; set; }

        private readonly ApplicationDbContext _context;
        public BansModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Blokada> bans {  get; set; } = new List<Blokada>();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Pobierz ID zalogowanego uøytkownika
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToPage("/Account/Login");
                }

                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return RedirectToPage("/Account/Login");
                }

                // Pobierz dane uøytkownika
                CurrentUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (CurrentUser == null)
                {
                    return RedirectToPage("/Account/Login");
                }

                // Sprawdü, czy uøytkownik jest administratorem
                //if (CurrentUser.Admin == null)
                //{
                //    return RedirectToPage("/Diet/Index");
                //}

                bans = await _context.Blokady.Take(10).ToListAsync();

                return Page();
            }
            catch (Exception ex)
            {
                // Logowanie b≥Ídu
                Console.WriteLine($"B≥πd podczas ≥adowania strony diety: {ex.Message}");
                return RedirectToPage("/Error", new { message = ex.Message });
            }
        }
    }
}
