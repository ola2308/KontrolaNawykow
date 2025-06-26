using KontrolaNawykow.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace KontrolaNawykow.Pages.Admin
{
    public class ReportsModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? View { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? Delete { get; set; }

        [BindProperty]
        public int? Duration { get; set; }

        [BindProperty]
        public String Description { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool? Nowe { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool? WTrakcie { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool? Zamkniete { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool? Odrzucone { get; set; }

        [BindProperty(SupportsGet = true)]
        public int FilterStatus { get; set; } = 0;

        public int PageCount { get; set; }

        public User CurrentUser { get; set; }

        public List<Zgloszenie> reports { get; set; } = new List<Zgloszenie>();

        public Zgloszenie? displayedReport { get; set; }

        private readonly ApplicationDbContext _context;
        public ReportsModel(ApplicationDbContext context)
        {
            _context = context;
        }

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

                if (Delete != null)
                {
                    _context.Zgloszenia.Where(r => r.Id == Delete).ExecuteDelete();
                    return RedirectToPage("/Admin/Reports");
                }

                if(FilterStatus == 0)
                {
                    Nowe = true;
                    WTrakcie = true;
                }
                
                var zgloszenia = _context.Zgloszenia.Where(b => (Nowe.HasValue && b.Status == StatusZgloszenia.Nowe) ||
                (WTrakcie.HasValue && b.Status == StatusZgloszenia.WTrakcie) ||
                (Zamkniete.HasValue && b.Status == StatusZgloszenia.Zamkniete) ||
                (Odrzucone.HasValue && b.Status == StatusZgloszenia.Odrzucone));

                var records = await zgloszenia.CountAsync();
                if (records > 0) PageCount = --records / 10 + 1;

                if (PageNumber < 1) PageNumber = 1;
                reports = await (Search == null ? zgloszenia.Include(r => r.Zglaszajacy).Include(r => r.Zglaszany).Skip((PageNumber - 1) * 10).Take(10).ToListAsync()
                     : zgloszenia.Include(r => r.Zglaszajacy).Include(r => r.Zglaszany).Where(r => r.Zglaszajacy.Username == Search || r.Zglaszany.Username == Search).Skip((PageNumber - 1) * 10).Take(10).ToListAsync());


                if (View != null)
                {
                    displayedReport = await (_context.Zgloszenia.Include(r => r.Zglaszajacy).Include(r => r.Zglaszany).Where(r => r.Id == View).FirstAsync());
                }
                else displayedReport = null;

                return Page();
            }
            catch (Exception ex)
            {
                // Logowanie b³êdu
                Console.WriteLine($"B³¹d podczas ³adowania strony diety: {ex.Message}");
                return RedirectToPage("/Error", new { message = ex.Message });
            }
        }

        public async Task<IActionResult> OnPostAsync()
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
                
                var adminId = CurrentAdmin.First().Id;

                Zgloszenie report = await(_context.Zgloszenia.Include(z => z.Zglaszajacy).Where(z => z.Id == View).SingleAsync());

                if (report.Status == StatusZgloszenia.Zamkniete || report.Status == StatusZgloszenia.Odrzucone) return Redirect("Reports");

                Blokada ban = new Blokada
                {
                    UzytkownikId = (int)report.IdZglaszanego,
                    AdminId = adminId,
                    Powod = Description,
                    DataPoczatku = DateTime.UtcNow,
                    DataKonca = DateTime.UtcNow.AddDays((int)Duration)
                };

                report.Status = StatusZgloszenia.Zamkniete;

                _context.Blokady.Add(ban);
                _context.Zgloszenia.Update(report);
                await _context.SaveChangesAsync();

                return Redirect("Reports");
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
