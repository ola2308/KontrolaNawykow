using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using KontrolaNawykow.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace KontrolaNawykow.Pages.Account.Dietitian
{
    [Authorize(Roles = "Dietitian")]
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public DashboardModel(ApplicationDbContext context) => _context = context;

        public List<User> PendingClients { get; set; }
        public List<User> AcceptedClients { get; set; }

        public async Task OnGetAsync()
        {
            var dietId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            PendingClients = await _context.Users
                .Where(u => u.DietetykId == dietId && u.DieticianAccepted != 1)
                .ToListAsync();

            AcceptedClients = await _context.Users
                .Where(u => u.DietetykId == dietId && u.DieticianAccepted == 1)
                .ToListAsync();
        }
    }
}
