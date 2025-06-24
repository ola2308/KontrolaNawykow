using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using KontrolaNawykow.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace KontrolaNawykow.Pages.Recipes
{
    [Authorize]
    public class AddModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public AddModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        [Required(ErrorMessage = "Nazwa przepisu jest wymagana")]
        [StringLength(200, ErrorMessage = "Nazwa przepisu nie mo�e by� d�u�sza ni� 200 znak�w")]
        public string Name { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Kalorie s� wymagane")]
        [Range(0, 9999, ErrorMessage = "Kalorie musz� by� mi�dzy 0 a 9999")]
        public int Calories { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Bia�ko jest wymagane")]
        [Range(0, 100, ErrorMessage = "Bia�ko musi by� mi�dzy 0 a 100g")]
        public float Protein { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "W�glowodany s� wymagane")]
        [Range(0, 100, ErrorMessage = "W�glowodany musz� by� mi�dzy 0 a 100g")]
        public float Carbs { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "T�uszcze s� wymagane")]
        [Range(0, 100, ErrorMessage = "T�uszcze musz� by� mi�dzy 0 a 100g")]
        public float Fat { get; set; }

        [BindProperty]
        [StringLength(5000, ErrorMessage = "Instrukcje nie mog� by� d�u�sze ni� 5000 znak�w")]
        public string Instructions { get; set; }

        [BindProperty]
        public bool IsPublic { get; set; }

        [BindProperty]
        public IFormFile Image { get; set; }

        [BindProperty]
        public string RecipeIngredients { get; set; }

        public string SuccessMessage { get; set; }
        public string ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Sprawd� czy u�ytkownik jest zalogowany
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                Console.WriteLine("=== POST Recipe Add started ===");
                Console.WriteLine($"ModelState.IsValid: {ModelState.IsValid}");

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    Console.WriteLine("User not authenticated");
                    return RedirectToPage("/Account/Login");
                }

                Console.WriteLine($"User ID: {userId}");

                // Debugowanie danych formularza
                Console.WriteLine($"Name: '{Name}'");
                Console.WriteLine($"Calories: {Calories}");
                Console.WriteLine($"Protein: {Protein}");
                Console.WriteLine($"Carbs: {Carbs}");
                Console.WriteLine($"Fat: {Fat}");
                Console.WriteLine($"IsPublic: {IsPublic}");
                Console.WriteLine($"Instructions length: {Instructions?.Length ?? 0}");
                Console.WriteLine($"Image: {Image?.FileName ?? "null"} ({Image?.Length ?? 0} bytes)");
                Console.WriteLine($"RecipeIngredients: '{RecipeIngredients}'");

                // Debug ModelState errors
                if (!ModelState.IsValid)
                {
                    Console.WriteLine("ModelState errors:");
                    foreach (var modelState in ModelState)
                    {
                        foreach (var error in modelState.Value.Errors)
                        {
                            Console.WriteLine($"- {modelState.Key}: {error.ErrorMessage}");
                        }
                    }
                    ErrorMessage = "Prosz� poprawi� b��dy w formularzu.";
                    return Page();
                }

                // Rozpocznij transakcj�
                using var transaction = await _context.Database.BeginTransactionAsync();
                Console.WriteLine("Transaction started");

                try
                {
                    // Utw�rz nowy przepis
                    var recipe = new Recipe
                    {
                        Name = Name,
                        Calories = Calories,
                        Protein = Protein,
                        Carbs = Carbs,
                        Fat = Fat,
                        Instructions = Instructions ?? string.Empty,
                        IsPublic = IsPublic,
                        UserId = int.Parse(userId)
                    };

                    Console.WriteLine("Recipe object created");

                    // Obs�uga obrazu
                    if (Image != null && Image.Length > 0)
                    {
                        Console.WriteLine($"Processing image: {Image.FileName}, Size: {Image.Length}");

                        // Sprawd� typ pliku
                        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
                        if (!allowedTypes.Contains(Image.ContentType.ToLower()))
                        {
                            Console.WriteLine($"Invalid image type: {Image.ContentType}");
                            ErrorMessage = "Dozwolone s� tylko pliki obraz�w (JPG, PNG, GIF, WebP).";
                            return Page();
                        }

                        // Sprawd� rozmiar pliku (max 5MB)
                        if (Image.Length > 5 * 1024 * 1024)
                        {
                            Console.WriteLine($"Image too large: {Image.Length} bytes");
                            ErrorMessage = "Plik nie mo�e by� wi�kszy ni� 5MB.";
                            return Page();
                        }

                        using (var memoryStream = new MemoryStream())
                        {
                            await Image.CopyToAsync(memoryStream);
                            recipe.ImageData = memoryStream.ToArray();
                        }

                        Console.WriteLine("Image processed successfully");
                    }
                    else
                    {
                        Console.WriteLine("No image provided");
                    }

                    // Dodaj przepis do bazy
                    _context.Recipes.Add(recipe);
                    await _context.SaveChangesAsync();

                    Console.WriteLine($"Recipe saved with ID: {recipe.Id}");

                    // Przetw�rz sk�adniki
                    if (!string.IsNullOrEmpty(RecipeIngredients))
                    {
                        Console.WriteLine("Processing ingredients...");

                        try
                        {
                            var ingredientsData = JsonSerializer.Deserialize<List<RecipeIngredientDto>>(RecipeIngredients);
                            Console.WriteLine($"Parsed {ingredientsData?.Count ?? 0} ingredients");

                            if (ingredientsData != null && ingredientsData.Count > 0)
                            {
                                // Filtruj nieprawid�owe sk�adniki
                                var validIngredients = ingredientsData
                                    .Where(ing => ing.IngredientId > 0 && ing.Amount > 0)
                                    .ToList();

                                Console.WriteLine($"Valid ingredients: {validIngredients.Count}");

                                if (validIngredients.Count == 0)
                                {
                                    Console.WriteLine("No valid ingredients found");
                                    await transaction.RollbackAsync();
                                    ErrorMessage = "Nie znaleziono prawid�owych sk�adnik�w. Dodaj przynajmniej jeden sk�adnik z ilo�ci�.";
                                    return Page();
                                }

                                foreach (var ingredient in validIngredients)
                                {
                                    Console.WriteLine($"Processing ingredient: ID={ingredient.IngredientId}, Amount={ingredient.Amount}");

                                    // Sprawd� czy sk�adnik istnieje w bazie
                                    var ingredientExists = await _context.Ingredients
                                        .AnyAsync(i => i.Id == ingredient.IngredientId);

                                    if (!ingredientExists)
                                    {
                                        Console.WriteLine($"Ingredient with ID {ingredient.IngredientId} does not exist");
                                        await transaction.RollbackAsync();
                                        ErrorMessage = $"Sk�adnik o ID {ingredient.IngredientId} nie istnieje w bazie danych.";
                                        return Page();
                                    }

                                    var recipeIngredient = new RecipeIngredient
                                    {
                                        RecipeId = recipe.Id,
                                        IngredientId = ingredient.IngredientId,
                                        Amount = ingredient.Amount
                                    };

                                    _context.RecipeIngredients.Add(recipeIngredient);
                                    Console.WriteLine($"Added ingredient: ID={ingredient.IngredientId}, Amount={ingredient.Amount}");
                                }

                                await _context.SaveChangesAsync();
                                Console.WriteLine("All ingredients saved successfully");
                            }
                            else
                            {
                                Console.WriteLine("No ingredients data provided");
                            }
                        }
                        catch (JsonException ex)
                        {
                            Console.WriteLine($"JSON parsing error: {ex.Message}");
                            await transaction.RollbackAsync();
                            ErrorMessage = "B��d podczas przetwarzania sk�adnik�w. Sprawd� format danych.";
                            return Page();
                        }
                    }
                    else
                    {
                        Console.WriteLine("No ingredients JSON provided");
                    }

                    // Zatwierd� transakcj�
                    await transaction.CommitAsync();
                    Console.WriteLine("Transaction committed successfully");

                    SuccessMessage = $"Przepis '{Name}' zosta� dodany pomy�lnie!";

                    // Wyczy�� formularz po pomy�lnym dodaniu
                    Name = string.Empty;
                    Calories = 0;
                    Protein = 0;
                    Carbs = 0;
                    Fat = 0;
                    Instructions = string.Empty;
                    IsPublic = false;
                    RecipeIngredients = string.Empty;

                    // Redirect to diet page to see the new recipe
                    TempData["SuccessMessage"] = SuccessMessage;
                    return RedirectToPage("/Diet/Index");
                }
                catch (Exception innerEx)
                {
                    Console.WriteLine($"Error in transaction: {innerEx.Message}");
                    Console.WriteLine($"Stack trace: {innerEx.StackTrace}");
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OnPostAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                ErrorMessage = $"Wyst�pi� b��d podczas dodawania przepisu: {ex.Message}";
                return Page();
            }
        }

        // DTO dla sk�adnik�w przepisu
        public class RecipeIngredientDto
        {
            public int IngredientId { get; set; }
            public float Amount { get; set; }
        }

        public bool adminCheck()
        {
            return false;
            //var CurrentAdmin = _context.Admins.Where(a => a.UzytkownikId == CurrentUser.Id);
            //return CurrentAdmin.Any();
        }
    }
}