using KontrolaNawykow.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace KontrolaNawykow.Pages.DieticianConnection
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public User CurrentUser { get; set; }
        public Dietetyk ConnectedDietician { get; set; }

        [BindProperty(SupportsGet = true)]
        public string RemoveDietician { get; set; }

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

                if (RemoveDietician != null)
                {
                    CurrentUser.DietetykId = null;
                    await _context.SaveChangesAsync();
                }
                else
                {
                    ConnectedDietician = await _context.Dietetycy
                        .FirstOrDefaultAsync(d => d.Id == CurrentUser.DietetykId);
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
