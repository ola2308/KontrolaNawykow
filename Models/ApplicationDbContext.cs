using Microsoft.EntityFrameworkCore;

namespace KontrolaNawykow.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Recipe> Recipes { get; set; }
        public DbSet<Ingredient> Ingredients { get; set; }
        public DbSet<RecipeIngredient> RecipeIngredients { get; set; }
        public DbSet<MealPlan> MealPlans { get; set; }
        public DbSet<CustomFood> CustomFoods { get; set; }
        public DbSet<ShoppingList> ShoppingLists { get; set; }
        public DbSet<ToDo> ToDos { get; set; }
        public DbSet<Dietetyk> Dietetycy { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Blokada> Blokady { get; set; }
        public DbSet<Zgloszenie> Zgloszenia { get; set; }
        public DbSet<Nawyk> Nawyki { get; set; }
        public DbSet<NawykWPlanie> NawykiWPlanie { get; set; }
        public DbSet<Statystyki> Statystyki { get; set; }
        public DbSet<PlanPosilkowPrzepis> PlanPosilkowPrzepisy { get; set; }
        public DbSet<ListaZakupow> ListyZakupow { get; set; }
        public DbSet<RecipeRating> RecipeRatings { get; set; }
        public DbSet<DietetykRating> DietetykRatings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<RecipeIngredient>()
                .HasOne(ri => ri.Recipe)
                .WithMany(r => r.RecipeIngredients)
                .HasForeignKey(ri => ri.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RecipeIngredient>()
                .HasOne(ri => ri.Ingredient)
                .WithMany(i => i.RecipeIngredients)
                .HasForeignKey(ri => ri.IngredientId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Recipe>()
                .HasOne(r => r.User)
                .WithMany(u => u.Recipes)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MealPlan>()
                .HasOne(mp => mp.User)
                .WithMany(u => u.MealPlans)
                .HasForeignKey(mp => mp.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MealPlan>()
                .HasOne(mp => mp.Recipe)
                .WithMany(r => r.MealPlans)
                .HasForeignKey(mp => mp.RecipeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CustomFood>()
                .HasOne(cf => cf.User)
                .WithMany(u => u.CustomFoods)
                .HasForeignKey(cf => cf.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ShoppingList>()
                .HasOne(sl => sl.User)
                .WithMany(u => u.ShoppingLists)
                .HasForeignKey(sl => sl.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ToDo>()
                .HasOne(td => td.User)
                .WithMany(u => u.ToDos)
                .HasForeignKey(td => td.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasOne(u => u.Dietetyk)
                .WithMany(d => d.Users)
                .HasForeignKey(u => u.DietetykId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Admin>()
                .HasOne(a => a.Uzytkownik)
                .WithOne(u => u.Admin)
                .HasForeignKey<Admin>(a => a.UzytkownikId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Blokada>()
                .HasOne(b => b.Admin)
                .WithMany(a => a.Blokady)
                .HasForeignKey(b => b.AdminId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Blokada>()
                .HasOne(b => b.Uzytkownik)
                .WithMany()
                .HasForeignKey(b => b.UzytkownikId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Zgloszenie>()
                .HasOne(z => z.Zglaszajacy)
                .WithMany()
                .HasForeignKey(z => z.IdZglaszajacego)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Zgloszenie>()
                .HasOne(z => z.Zglaszany)
                .WithMany()
                .HasForeignKey(z => z.IdZglaszanego)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Zgloszenie>()
                .HasOne(z => z.Blokada)
                .WithMany(b => b.Zgloszenia)
                .HasForeignKey(z => z.IdBlokady)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<NawykWPlanie>()
                .HasOne(nwp => nwp.Nawyk)
                .WithMany(n => n.NawykiWPlanie)
                .HasForeignKey(nwp => nwp.NawykId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<NawykWPlanie>()
                .HasOne(nwp => nwp.Uzytkownik)
                .WithMany(u => u.NawykiWPlanie)
                .HasForeignKey(nwp => nwp.UzytkownikId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Statystyki>()
                .HasOne(s => s.Uzytkownik)
                .WithMany(u => u.Statystyki)
                .HasForeignKey(s => s.UzytkownikId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PlanPosilkowPrzepis>()
                .HasOne(ppp => ppp.PlanPosilkow)
                .WithMany(pp => pp.PlanPosilkowPrzepisy)
                .HasForeignKey(ppp => ppp.PlanPosilkowId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PlanPosilkowPrzepis>()
                .HasOne(ppp => ppp.Przepis)
                .WithMany(r => r.PlanPosilkowPrzepisy)
                .HasForeignKey(ppp => ppp.PrzepisId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ListaZakupow>()
                .HasOne(lz => lz.PlanPosilkow)
                .WithMany(pp => pp.ListaZakupow)
                .HasForeignKey(lz => lz.PlanPosilkowId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RecipeRating>()
                .HasOne(rr => rr.Recipe)
                .WithMany(r => r.Ratings)
                .HasForeignKey(rr => rr.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RecipeRating>()
                .HasOne(rr => rr.User)
                .WithMany(u => u.RecipeRatings)
                .HasForeignKey(rr => rr.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DietetykRating>()
                .HasOne(dr => dr.Dietetyk)
                .WithMany(d => d.Ratings)
                .HasForeignKey(dr => dr.DietetykId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DietetykRating>()
                .HasOne(dr => dr.User)
                .WithMany(u => u.DietetykRatings)
                .HasForeignKey(dr => dr.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RecipeRating>()
                .HasIndex(rr => new { rr.RecipeId, rr.UserId })
                .IsUnique();

            modelBuilder.Entity<DietetykRating>()
                .HasIndex(dr => new { dr.DietetykId, dr.UserId })
                .IsUnique();

            if (Database.IsSqlite())
            {
                modelBuilder.Entity<User>()
                    .Property(u => u.CreatedAt)
                    .HasDefaultValueSql("datetime('now')");

                modelBuilder.Entity<ToDo>()
                    .Property(t => t.CreatedAt)
                    .HasDefaultValueSql("datetime('now')");

                modelBuilder.Entity<Zgloszenie>()
                    .Property(z => z.Data)
                    .HasDefaultValueSql("datetime('now')");

                modelBuilder.Entity<Nawyk>()
                    .Property(n => n.DataUtworzenia)
                    .HasDefaultValueSql("datetime('now')");

                modelBuilder.Entity<RecipeRating>()
                    .Property(rr => rr.CreatedAt)
                    .HasDefaultValueSql("datetime('now')");

                modelBuilder.Entity<DietetykRating>()
                    .Property(dr => dr.CreatedAt)
                    .HasDefaultValueSql("datetime('now')");
            }
        }
    }
}