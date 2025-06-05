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

        public Blokada? displayedBan { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? View {  get; set; }

        [BindProperty(SupportsGet = true)]
        public int? Delete { get; set; }

        [BindProperty]
        public int? Duration { get; set; }

        public int PageCount { get; set; }

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

                if(Delete != null)
                {
                    _context.Blokady.Where(ban => ban.Id == Delete).ExecuteDelete();
                    return RedirectToPage("/Admin/Bans");
                }

                var records = await _context.Blokady.CountAsync();
                if (records > 0) PageCount = --records / 10 + 1;
                
                if (PageNumber < 1) PageNumber = 1;
                bans = await (Search == null ? _context.Blokady.Include(ban => ban.Uzytkownik).Include(ban => ban.Admin).Skip((PageNumber-1)*10).Take(10).ToListAsync()
                     : _context.Blokady.Include(ban => ban.Uzytkownik).Include(ban => ban.Admin).Where(ban => ban.Uzytkownik.Username == Search || ban.Admin.Uzytkownik.Username == Search).Skip((PageNumber - 1) * 10).Take(10).ToListAsync());

                if(View !=  null)
                {
                    displayedBan = await (_context.Blokady.Include(ban => ban.Uzytkownik).Include(ban => ban.Admin).Where(ban => ban.Id == View).FirstAsync());
                }
                else displayedBan = null;

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
