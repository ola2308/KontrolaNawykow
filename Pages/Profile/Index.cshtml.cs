﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using KontrolaNawykow.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text;

namespace KontrolaNawykow.Pages.Profile
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // -------------------------------
        // Własna klasa DTO (ViewModel) 
        // -------------------------------
        // Nie zapisywana w bazie – służy tylko do wyświetlenia listy zakupów
        public class ShoppingListItem
        {
            public int IngredientId { get; set; }
            public string IngredientName { get; set; }
            public float TotalAmount { get; set; }
        }

        // ------------------------------------------------------------
        // Właściwości (pola) widoku
        // ------------------------------------------------------------
        public User CurrentUser { get; set; }
        public double BMI { get; set; }
        public string BMICategory { get; set; }
        public int TotalCalories { get; set; }
        public int TotalRecipes { get; set; }
        public int TotalMealPlans { get; set; }
        public int CompletedTasks { get; set; }
        public int PendingTasks { get; set; }

        // Zmieniamy typ z List<Ingredient> na List<ShoppingListItem>
        public List<ShoppingListItem> ShoppingList { get; set; } = new List<ShoppingListItem>();

        // ------------------------------------------------------------
        // OnGetAsync – pobieranie danych użytkownika, obliczenia BMI itd.
        // ------------------------------------------------------------
        public async Task<IActionResult> OnGetAsync()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Pobierz dane użytkownika
            CurrentUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (CurrentUser == null)
            {
                return RedirectToPage("/Account/Login");
            }

            // Oblicz BMI (jeśli waga i wzrost są wypełnione)
            if (CurrentUser.Wzrost.HasValue && CurrentUser.Waga.HasValue)
            {
                double heightInMeters = CurrentUser.Wzrost.Value / 100.0;
                BMI = CurrentUser.Waga.Value / (heightInMeters * heightInMeters);
                BMICategory = GetBMICategory(BMI);
            }

            // Statystyki
            TotalRecipes = await _context.Recipes.CountAsync(r => r.UserId == userId);
            TotalMealPlans = await _context.MealPlans.CountAsync(mp => mp.UserId == userId);
            CompletedTasks = await _context.ToDos.CountAsync(t => t.UserId == userId && t.IsCompleted);
            PendingTasks = await _context.ToDos.CountAsync(t => t.UserId == userId && !t.IsCompleted);

            // Liczba kalorii z zaplanowanych posiłków na dziś
            var today = DateTime.Today;

            // Pobierz plany posiłków zawierające oba rodzaje relacji (stara bezpośrednia i nowa przez tabelę łączącą)
            var meals = await _context.MealPlans
                .Where(mp => mp.UserId == userId && mp.Date.HasValue && mp.Date.Value.Date == today)
                .Include(mp => mp.Recipe)
                .Include(mp => mp.PlanPosilkowPrzepisy)
                    .ThenInclude(ppp => ppp.Przepis)
                .ToListAsync();

            foreach (var meal in meals)
            {
                // Preferuj stary bezpośredni sposób dostępu
                var recipe = meal.Recipe;

                // Jeśli nie ma bezpośredniej relacji, używamy relacji przez tabelę łączącą
                if (recipe == null && meal.PlanPosilkowPrzepisy.Any())
                {
                    recipe = meal.PlanPosilkowPrzepisy.First().Przepis;
                }

                if (recipe != null)
                {
                    TotalCalories += recipe.Calories;
                }
            }

            // Pobierz listę zakupów – na najbliższy tydzień
            ShoppingList = await GetShoppingListAsync(userId);

            return Page();
        }

        // ------------------------------------------------------------
        // Obliczanie kategorii BMI
        // ------------------------------------------------------------
        private string GetBMICategory(double bmi)
        {
            if (bmi < 16)
                return "Wygłodzenie";
            else if (bmi < 17)
                return "Wychudzenie";
            else if (bmi < 18.5)
                return "Niedowaga";
            else if (bmi < 25)
                return "Waga prawidłowa";
            else if (bmi < 30)
                return "Nadwaga";
            else if (bmi < 35)
                return "Otyłość I stopnia";
            else if (bmi < 40)
                return "Otyłość II stopnia";
            else
                return "Otyłość III stopnia";
        }

        // ------------------------------------------------------------
        // Generowanie listy zakupów z najbliższego tygodnia
        // Obsługuje zarówno starą relację bezpośrednią, jak i nową przez tabelę łączącą
        // ------------------------------------------------------------
        private async Task<List<ShoppingListItem>> GetShoppingListAsync(int userId)
        {
            var startDate = DateTime.Today;
            var endDate = startDate.AddDays(7);

            // Słownik: klucz = IngredientId, wartość = ShoppingListItem (sumujemy Amount)
            var shoppingDict = new Dictionary<int, ShoppingListItem>();

            // 1. Pobierz plany posiłków używając starej bezpośredniej relacji
            var mealPlans = await _context.MealPlans
                .Where(mp => mp.UserId == userId
                             && mp.Date.HasValue
                             && mp.Date.Value >= startDate
                             && mp.Date.Value <= endDate
                             && mp.RecipeId.HasValue)
                .Include(mp => mp.Recipe)
                    .ThenInclude(r => r.RecipeIngredients)
                        .ThenInclude(ri => ri.Ingredient)
                .ToListAsync();

            // Przetwarzanie ze starej relacji
            foreach (var meal in mealPlans)
            {
                if (meal.Recipe?.RecipeIngredients == null)
                    continue;

                foreach (var recipeIngredient in meal.Recipe.RecipeIngredients)
                {
                    if (recipeIngredient.Ingredient != null)
                    {
                        float amount = recipeIngredient.Amount ?? 0f;

                        if (shoppingDict.TryGetValue(recipeIngredient.IngredientId, out var existing))
                        {
                            // Dodajemy do już istniejącej sumy
                            existing.TotalAmount += amount;
                        }
                        else
                        {
                            // Tworzymy nowy obiekt ShoppingListItem
                            shoppingDict[recipeIngredient.IngredientId] = new ShoppingListItem
                            {
                                IngredientId = recipeIngredient.IngredientId,
                                IngredientName = recipeIngredient.Ingredient.Name,
                                TotalAmount = amount
                            };
                        }
                    }
                }
            }

            // 2. Pobierz również plany korzystające z nowej relacji przez PlanPosilkowPrzepisy
            var newMealPlans = await _context.MealPlans
                .Where(mp => mp.UserId == userId
                             && mp.Date.HasValue
                             && mp.Date.Value >= startDate
                             && mp.Date.Value <= endDate
                             && mp.PlanPosilkowPrzepisy.Any())
                .Include(mp => mp.PlanPosilkowPrzepisy)
                    .ThenInclude(ppp => ppp.Przepis)
                        .ThenInclude(r => r.RecipeIngredients)
                            .ThenInclude(ri => ri.Ingredient)
                .ToListAsync();

            // Przetwarzanie z nowej relacji
            foreach (var meal in newMealPlans)
            {
                foreach (var planPosilkowPrzepis in meal.PlanPosilkowPrzepisy)
                {
                    if (planPosilkowPrzepis.Przepis?.RecipeIngredients == null)
                        continue;

                    foreach (var recipeIngredient in planPosilkowPrzepis.Przepis.RecipeIngredients)
                    {
                        // Unikamy podwójnego liczenia składników, które już są w słowniku
                        // z tego samego przepisu przez starą relację
                        if (meal.RecipeId == planPosilkowPrzepis.PrzepisId)
                            continue;

                        if (recipeIngredient.Ingredient != null)
                        {
                            float amount = recipeIngredient.Amount ?? 0f;

                            if (shoppingDict.TryGetValue(recipeIngredient.IngredientId, out var existing))
                            {
                                // Dodajemy do już istniejącej sumy
                                existing.TotalAmount += amount;
                            }
                            else
                            {
                                // Tworzymy nowy obiekt ShoppingListItem
                                shoppingDict[recipeIngredient.IngredientId] = new ShoppingListItem
                                {
                                    IngredientId = recipeIngredient.IngredientId,
                                    IngredientName = recipeIngredient.Ingredient.Name,
                                    TotalAmount = amount
                                };
                            }
                        }
                    }
                }
            }

            return shoppingDict.Values.ToList();
        }

        // ------------------------------------------------------------
        // Wygenerowanie pliku (np. .txt) z listą zakupów
        // ------------------------------------------------------------
        public async Task<IActionResult> OnPostGenerateShoppingListAsync()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var shoppingItems = await GetShoppingListAsync(userId);

            // Prosty przykład – tworzymy tekst do pobrania
            var shoppingListText = "Lista zakupów na nadchodzący tydzień:\n\n";
            foreach (var item in shoppingItems)
            {
                shoppingListText += $"- {item.IngredientName}: {item.TotalAmount} g\n";
            }

            // Konwersja do bajtów
            byte[] fileBytes = Encoding.UTF8.GetBytes(shoppingListText);

            // Zwróć plik do pobrania jako plain text
            return File(fileBytes, "text/plain", "lista_zakupow.txt");
        }
    }
}

sa