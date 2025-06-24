using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using KontrolaNawykow.Models;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.CodeDom;

namespace KontrolaNawykow.Pages.Fridge
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string Name { get; set; }

        [BindProperty]
        public int Amount { get; set; }

        [BindProperty]
        public string Unit { get; set; }

        [BindProperty]
        public List<int> SelectedIds { get; set; } = new();

        public List<FridgeItem> FridgeItems { get; set; } = new();

        public List<ShoppingList> ShoppingLists { get; set; } = new();

        public async Task OnGetAsync()
        {
            await LoadDataAsync();
        }

        public async Task<IActionResult> OnPostAddAsync()
        {
            var userId = GetCurrentUserId();
            Console.WriteLine($"Dodawanie: {Name}, {Amount}, {Unit}");

            var item = new FridgeItem
            {
                Name = Name,
                Amount = Amount,
                Unit = Unit,
                UserId = GetCurrentUserId()
            };

            foreach (var kvp in ModelState)
            {
                foreach(var err in kvp.Value.Errors)
                {
                    Console.WriteLine($"Error in {kvp.Key}: {err.ErrorMessage}");
                }
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            Console.WriteLine(">>> DB filr = " + _context.Database.GetDbConnection().DataSource);

            _context.FridgeItems.Add(item);
            await _context.SaveChangesAsync();


            return RedirectToPage();
        }

        public async Task LoadDataAsync()
        {
            var userId = GetCurrentUserId();

            FridgeItems = await _context.FridgeItems
                .Where(f => f.UserId == userId)
                .ToListAsync();

            ShoppingLists = await _context.ShoppingLists
                .Include(s => s.Ingredient)
                .Where(s => s.UserId == userId)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var item = await _context.FridgeItems.FindAsync(id);

            Console.WriteLine($"\n>>> usuwam produkt o ID: {id}");

           if(item != null)
            {
                _context.FridgeItems.Remove(item);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage();
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        }
    }
}
