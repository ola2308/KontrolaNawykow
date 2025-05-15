using KontrolaNawykow.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace KontrolaNawykow.Pages.Admin
{
    public class ReportsModel : PageModel
    {
        public User CurrentUser { get; set; }

        private readonly ApplicationDbContext _context;
        public ReportsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Zgloszenie> reports { get; set; } = new List<Zgloszenie>();

        [BindProperty(SupportsGet = true)]
        public int page { get; set; } = 0;

        public int pageTotal { get; set; } = 0;

        //[BindProperty(SupportsGet = false)]
        //public String search;

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

                // SprawdŸ, czy u¿ytkownik jest administratorem
                var CurrentAdmin = _context.Admins.Where(a => a.UzytkownikId == CurrentUser.Id);
                if (!CurrentAdmin.Any())
                {
                    return RedirectToPage("/Diet/Index");
                }

                reports = await _context.Zgloszenia.Take(10).ToListAsync();

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
