using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KontrolaNawykow.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Username { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
        public Gender? Plec { get; set; }

        public int? Wiek { get; set; }
        public double? Waga { get; set; }
        public double? Wzrost { get; set; }

        [MaxLength(50)]
        public string? AktywnoscFizyczna { get; set; }

        [MaxLength(50)]
        public string? RodzajPracy { get; set; }

        public UserGoal? Cel { get; set; }
        public double? CustomBmi { get; set; }
        public int? CustomCaloriesDeficit { get; set; }
        public int? CustomProteinGrams { get; set; }
        public int? CustomCarbsGrams { get; set; }
        public int? CustomFatGrams { get; set; }

        public int? DietetykId { get; set; }
        [ForeignKey("DietetykId")]
        public Dietetyk Dietetyk { get; set; }

        public List<Recipe> Recipes { get; set; }
        public List<MealPlan> MealPlans { get; set; }
        public List<CustomFood> CustomFoods { get; set; }
        public List<ShoppingList> ShoppingLists { get; set; }
        public List<ToDo> ToDos { get; set; }
        public List<Statystyki> Statystyki { get; set; }
        public List<NawykWPlanie> NawykiWPlanie { get; set; }
        public Admin Admin { get; set; }
    }

    public enum Gender
    {
        Mezczyzna,
        Kobieta
    }

    public enum UserGoal
    {
        ZdroweNawyki,
        Schudniecie,
        PrzybranieMasy
    }

    public class Recipe
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public int Calories { get; set; }
        public float Protein { get; set; }
        public float Fat { get; set; }
        public float Carbs { get; set; }
        public string Instructions { get; set; }
        public byte[] ImageData { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public bool IsPublic { get; set; }

        public List<RecipeIngredient> RecipeIngredients { get; set; }

        // Stara relacja (dla kompatybilności z istniejącym kodem)
        public List<MealPlan> MealPlans { get; set; }

        // Nowa relacja przez tabelę łączącą
        public List<PlanPosilkowPrzepis> PlanPosilkowPrzepisy { get; set; }
    }

    public class RecipeIngredient
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Recipe")]
        public int RecipeId { get; set; }
        public Recipe Recipe { get; set; }

        [ForeignKey("Ingredient")]
        public int IngredientId { get; set; }
        public Ingredient Ingredient { get; set; }

        public float? Amount { get; set; }
    }

    public class Ingredient
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public int Calories { get; set; }
        public float Protein { get; set; }
        public float Fat { get; set; }
        public float Carbs { get; set; }

        public List<RecipeIngredient> RecipeIngredients { get; set; }
        public List<ShoppingList> ShoppingLists { get; set; }
    }

    public class MealPlan
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public MealType MealType { get; set; }
        public DateTime? Date { get; set; }

        // Stara relacja (dla kompatybilności z istniejącym kodem)
        public int? RecipeId { get; set; }
        [ForeignKey("RecipeId")]
        public Recipe Recipe { get; set; }

        public string CustomEntry { get; set; }
        public bool Eaten { get; set; }

        // Nowa relacja przez tabelę łączącą
        public List<PlanPosilkowPrzepis> PlanPosilkowPrzepisy { get; set; }
        public List<ListaZakupow> ListaZakupow { get; set; }
    }

    public class PlanPosilkowPrzepis
    {
        [Key]
        public int Id { get; set; }

        public int PlanPosilkowId { get; set; }
        public MealPlan PlanPosilkow { get; set; }

        public int PrzepisId { get; set; }
        public Recipe Przepis { get; set; }
    }

    public enum MealType
    {
        Sniadanie,
        Obiad,
        Kolacja,
        Przekaska
    }

    public class CustomFood
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public string Name { get; set; }
        public int Calories { get; set; }
        public float Protein { get; set; }
        public float Fat { get; set; }
        public float Carbs { get; set; }
    }

    public class ShoppingList
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public int IngredientId { get; set; }
        public Ingredient Ingredient { get; set; }

        public float Amount { get; set; }
        public bool Status { get; set; }
    }

    public class ListaZakupow
    {
        [Key]
        public int Id { get; set; }

        public int PlanPosilkowId { get; set; }
        public MealPlan PlanPosilkow { get; set; }

        public StatusRodzaj Status { get; set; }
    }

    public enum StatusRodzaj
    {
        Niezakupione,
        Zakupione
    }

    public class ToDo
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public string Task { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsTemplate { get; set; } = false;
    }

    // Nowe klasy
    public class Dietetyk
    {
        [Key]
        public int Id { get; set; }

        public string Imie { get; set; }
        public string Nazwisko { get; set; }
        public string Email { get; set; }
        public byte[] Zdjecie { get; set; }
        public string Specjalizacja { get; set; }
        public string Telefon { get; set; }

        public List<User> Users { get; set; }
    }

    public class Admin
    {
        [Key]
        public int Id { get; set; }

        public int UzytkownikId { get; set; }
        [ForeignKey("UzytkownikId")]
        public User Uzytkownik { get; set; }

        public List<Blokada> Blokady { get; set; }
    }

    public class Blokada
    {
        [Key]
        public int Id { get; set; }

        public int UzytkownikId { get; set; }
        [ForeignKey("UzytkownikId")]
        public User Uzytkownik { get; set; }

        public int AdminId { get; set; }
        [ForeignKey("AdminId")]
        public Admin Admin { get; set; }

        public DateTime DataPoczatku { get; set; }
        public DateTime? DataKonca { get; set; }
        public string Powod { get; set; }

        public List<Zgloszenie> Zgloszenia { get; set; }
    }

    public class Zgloszenie
    {
        [Key]
        public int Id { get; set; }

        public int IdZglaszajacego { get; set; }
        [ForeignKey("IdZglaszajacego")]
        public User Zglaszajacy { get; set; }

        public int? IdBlokady { get; set; }
        [ForeignKey("IdBlokady")]
        public Blokada Blokada { get; set; }

        public string Powod { get; set; }
        public DateTime Data { get; set; } = DateTime.UtcNow;
        public TypZgloszenia Typ { get; set; }
        public StatusZgloszenia Status { get; set; }

        public int? IdZglaszanego { get; set; }
        [ForeignKey("IdZglaszanego")]
        public User Zglaszany { get; set; }
    }

    public enum TypZgloszenia
    {
        Uzytkownik,
        Przepis,
        Komentarz
    }

    public enum StatusZgloszenia
    {
        Nowe,
        WTrakcie,
        Zamkniete,
        Odrzucone
    }

    public class Nawyk
    {
        [Key]
        public int Id { get; set; }

        public string Nazwa { get; set; }
        public bool Cykliczny { get; set; }
        public bool Wykonany { get; set; }
        public DateTime DataUtworzenia { get; set; } = DateTime.UtcNow;

        public List<NawykWPlanie> NawykiWPlanie { get; set; }
    }

    public class NawykWPlanie
    {
        [Key]
        public int Id { get; set; }

        public int NawykId { get; set; }
        [ForeignKey("NawykId")]
        public Nawyk Nawyk { get; set; }

        public DzienTygodnia Dzien { get; set; }

        public int UzytkownikId { get; set; }
        [ForeignKey("UzytkownikId")]
        public User Uzytkownik { get; set; }
    }

    public enum DzienTygodnia
    {
        Poniedzialek,
        Wtorek,
        Sroda,
        Czwartek,
        Piatek,
        Sobota,
        Niedziela
    }

    public class Statystyki
    {
        [Key]
        public int Id { get; set; }

        public int UzytkownikId { get; set; }
        [ForeignKey("UzytkownikId")]
        public User Uzytkownik { get; set; }

        public DateTime Data { get; set; }
        public double Waga { get; set; }
        public int DniDiety { get; set; }
        public double ZmianaWagi { get; set; }
    }

    public class UserStatisticsViewModel
    {
        public int UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public User User { get; set; }
        public int TotalRecipes { get; set; }
        public int TotalMealPlans { get; set; }
        public int CompletedTasks { get; set; }
        public int PendingTasks { get; set; }
    }
}