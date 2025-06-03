using KontrolaNawykow.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace KontrolaNawykow.Pages.YourDietician
{
    public class FindDieticiansModel : PageModel
    {
		private readonly ApplicationDbContext _context;

		public FindDieticiansModel(ApplicationDbContext context)
		{
			_context = context;
		}

		public User CurrentUser { get; set; }
		public List<Dietetyk> Dieticians { get; set; } = new List<Dietetyk>();

        [BindProperty(SupportsGet = true)]
        public string ApplyToDietician { get; set; }

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

				Dieticians = await _context.Dietetycy
					.ToListAsync();

				if (ApplyToDietician != null)
				{
					CurrentUser.DietetykId = int.Parse(ApplyToDietician);
                    await _context.SaveChangesAsync();
					return RedirectToPage("/YourDietician/Index");
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
