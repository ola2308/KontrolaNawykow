using System.Security.Claims;
using System.Threading.Tasks;
using KontrolaNawykow.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KontrolaNawykow.Pages.Account.Dietitian
{
    [Authorize(Roles = "Dietitian")]
    public class ClientDetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public ClientDetailsModel(ApplicationDbContext context) => _context = context;

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        public User Client { get; set; }

        [BindProperty]
        public int DieticianAccepted { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            Client = await _context.Users.FindAsync(Id);
            if (Client == null) return NotFound();

            DieticianAccepted = Client.DieticianAccepted ?? 0;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var client = await _context.Users.FindAsync(Id);
            if (client == null) return NotFound();

            client.DieticianAccepted = DieticianAccepted;
            await _context.SaveChangesAsync();
            return RedirectToPage("./Dashboard");
        }
    }
}
