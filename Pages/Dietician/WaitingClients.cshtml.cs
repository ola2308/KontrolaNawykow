using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using KontrolaNawykow.Models;

namespace KontrolaNawykow.Pages.Dietician
{
    [Authorize]
    public class WaitingClientsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public WaitingClientsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public User CurrentUser { get; set; }

        public Dietetyk CurrentDietician { get; set; }

        public List<User> Clients { get; set; } = new List<User>();

        [BindProperty(SupportsGet = true)]
        public string AcceptClient { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToPage("/Account/Login");
                }

                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return RedirectToPage("/Account/Login");
                }

                CurrentUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (CurrentUser == null)
                {
                    return RedirectToPage("/Account/Login");
                }

                CurrentDietician = await _context.Dietetycy
                    .FirstOrDefaultAsync(u => u.Id == 1);

                if (CurrentDietician == null)
                {
                    return RedirectToPage("/Account/Dietician/Login");
                }

                Clients = await _context.Users
                    .Where(u => u.DietetykId == 1)
                    .Where(u => u.DieticianAccepted == null)
                    .ToListAsync();

                if (AcceptClient != null)
                {
                    int ClientId = int.Parse(AcceptClient);
                    User Client = await _context.Users
                        .FirstOrDefaultAsync (u => u.Id == ClientId);

                    if (Client != null)
                    {
                        Client.DieticianAccepted = 1;
                        await _context.SaveChangesAsync();
                        return RedirectToPage("/Dietician/WaitingClients");
                    }
                }

                return Page();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"B³¹d podczas ³adowania strony YourDietician: {ex.Message}");
                return RedirectToPage("/Error");
            }
        }
    }
}
