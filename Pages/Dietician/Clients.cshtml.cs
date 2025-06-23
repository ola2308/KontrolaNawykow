using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using KontrolaNawykow.Models;

namespace KontrolaNawykow.Pages.Dietician
{
    [Authorize(Roles = "Dietitian")]
    public class ClientsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ClientsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public User CurrentUser { get; set; }

        public Dietetyk CurrentDietician { get; set; }

        public List<User> Clients { get; set; } = new List<User>();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToPage("/Account/Dietitian/Login");
                }

                var dietIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(dietIdClaim) || !int.TryParse(dietIdClaim, out int dietId))
                {
                    return RedirectToPage("/Account/Dietitian/Login");
                }

                CurrentDietician = await _context.Dietetycy
                    .FirstOrDefaultAsync(u => u.Id == dietId);

                if (CurrentDietician == null)
                {
                    return RedirectToPage("/Account/Dietician/Login");
                }

                Clients = await _context.Users
                    .Where(u => u.DietetykId == dietId)
                    .Where(u => u.DieticianAccepted != null)
                    .ToListAsync();

                return Page();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"B³¹d podczas ³adowania strony Dietician/Clients: {ex.Message}");
                return RedirectToPage("/Error");
            }
        }
    }
}
