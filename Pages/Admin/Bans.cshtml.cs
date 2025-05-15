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
                // Pobierz ID zalogowanego u¿ytkownika
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToPage("/Account/Login");
                }

                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return RedirectToPage("/Account/Login");
                }

                // Pobierz dane u¿ytkownika
                CurrentUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (CurrentUser == null)
                {
                    return RedirectToPage("/Account/Login");
                }

                //SprawdŸ, czy u¿ytkownik jest administratorem
                var CurrentAdmin = _context.Admins.Where(a => a.UzytkownikId == CurrentUser.Id);
                if (!CurrentAdmin.Any())
                {
                    return RedirectToPage("/Diet/Index");
                }

                //bans = await _context.Blokady.Take(10).ToListAsync();
                bans = await _context.Blokady.Include(ban => ban.Admin).Take(10).ToListAsync();

                return Page();
            }
            catch (Exception ex)
            {
                // Logowanie b³êdu
                Console.WriteLine($"B³¹d podczas ³adowania strony diety: {ex.Message}");
                return RedirectToPage("/Error", new { message = ex.Message });
            }
        }
    }
}
