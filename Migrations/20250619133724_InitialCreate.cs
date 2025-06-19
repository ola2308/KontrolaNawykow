using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KontrolaNawykow.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Dietetycy",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Imie = table.Column<string>(type: "TEXT", nullable: false),
                    Nazwisko = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    Zdjecie = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Specjalizacja = table.Column<string>(type: "TEXT", nullable: false),
                    Telefon = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dietetycy", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ingredients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Calories = table.Column<int>(type: "INTEGER", nullable: false),
                    Protein = table.Column<float>(type: "REAL", nullable: false),
                    Fat = table.Column<float>(type: "REAL", nullable: false),
                    Carbs = table.Column<float>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ingredients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Nawyki",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nazwa = table.Column<string>(type: "TEXT", nullable: false),
                    Cykliczny = table.Column<bool>(type: "INTEGER", nullable: false),
                    Wykonany = table.Column<bool>(type: "INTEGER", nullable: false),
                    DataUtworzenia = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nawyki", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: true, defaultValueSql: "datetime('now')"),
                    Plec = table.Column<int>(type: "INTEGER", nullable: true),
                    Wiek = table.Column<int>(type: "INTEGER", nullable: true),
                    Waga = table.Column<double>(type: "REAL", nullable: true),
                    Wzrost = table.Column<double>(type: "REAL", nullable: true),
                    AktywnoscFizyczna = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    RodzajPracy = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Cel = table.Column<int>(type: "INTEGER", nullable: true),
                    CustomBmi = table.Column<double>(type: "REAL", nullable: true),
                    CustomCaloriesDeficit = table.Column<int>(type: "INTEGER", nullable: true),
                    CustomProteinGrams = table.Column<int>(type: "INTEGER", nullable: true),
                    CustomCarbsGrams = table.Column<int>(type: "INTEGER", nullable: true),
                    CustomFatGrams = table.Column<int>(type: "INTEGER", nullable: true),
                    DietetykId = table.Column<int>(type: "INTEGER", nullable: true),
                    DieticianAccepted = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Dietetycy_DietetykId",
                        column: x => x.DietetykId,
                        principalTable: "Dietetycy",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Admins",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UzytkownikId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Admins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Admins_Users_UzytkownikId",
                        column: x => x.UzytkownikId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomFoods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Calories = table.Column<int>(type: "INTEGER", nullable: false),
                    Protein = table.Column<float>(type: "REAL", nullable: false),
                    Fat = table.Column<float>(type: "REAL", nullable: false),
                    Carbs = table.Column<float>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomFoods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomFoods_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DietetykRatings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DietetykId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Rating = table.Column<int>(type: "INTEGER", nullable: false),
                    Comment = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DietetykRatings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DietetykRatings_Dietetycy_DietetykId",
                        column: x => x.DietetykId,
                        principalTable: "Dietetycy",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DietetykRatings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NawykiWPlanie",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NawykId = table.Column<int>(type: "INTEGER", nullable: false),
                    Dzien = table.Column<int>(type: "INTEGER", nullable: false),
                    UzytkownikId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NawykiWPlanie", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NawykiWPlanie_Nawyki_NawykId",
                        column: x => x.NawykId,
                        principalTable: "Nawyki",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NawykiWPlanie_Users_UzytkownikId",
                        column: x => x.UzytkownikId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Recipes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Calories = table.Column<int>(type: "INTEGER", nullable: false),
                    Protein = table.Column<float>(type: "REAL", nullable: false),
                    Fat = table.Column<float>(type: "REAL", nullable: false),
                    Carbs = table.Column<float>(type: "REAL", nullable: false),
                    Instructions = table.Column<string>(type: "TEXT", nullable: false),
                    ImageData = table.Column<byte[]>(type: "BLOB", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsPublic = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recipes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Recipes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShoppingLists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    IngredientId = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<float>(type: "REAL", nullable: false),
                    Status = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShoppingLists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShoppingLists_Ingredients_IngredientId",
                        column: x => x.IngredientId,
                        principalTable: "Ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShoppingLists_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Statystyki",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UzytkownikId = table.Column<int>(type: "INTEGER", nullable: false),
                    Data = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Waga = table.Column<double>(type: "REAL", nullable: false),
                    DniDiety = table.Column<int>(type: "INTEGER", nullable: false),
                    ZmianaWagi = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Statystyki", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Statystyki_Users_UzytkownikId",
                        column: x => x.UzytkownikId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ToDos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Task = table.Column<string>(type: "TEXT", nullable: false),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    IsTemplate = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToDos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ToDos_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Blokady",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UzytkownikId = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminId = table.Column<int>(type: "INTEGER", nullable: false),
                    DataPoczatku = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DataKonca = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Powod = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Blokady", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Blokady_Admins_AdminId",
                        column: x => x.AdminId,
                        principalTable: "Admins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Blokady_Users_UzytkownikId",
                        column: x => x.UzytkownikId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MealPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    MealType = table.Column<int>(type: "INTEGER", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RecipeId = table.Column<int>(type: "INTEGER", nullable: true),
                    CustomEntry = table.Column<string>(type: "TEXT", nullable: false),
                    Eaten = table.Column<bool>(type: "INTEGER", nullable: false),
                    Gramature = table.Column<float>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MealPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MealPlans_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MealPlans_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecipeIngredients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RecipeId = table.Column<int>(type: "INTEGER", nullable: false),
                    IngredientId = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<float>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeIngredients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecipeIngredients_Ingredients_IngredientId",
                        column: x => x.IngredientId,
                        principalTable: "Ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecipeIngredients_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecipeRatings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RecipeId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Rating = table.Column<int>(type: "INTEGER", nullable: false),
                    Comment = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeRatings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecipeRatings_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecipeRatings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Zgloszenia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IdZglaszajacego = table.Column<int>(type: "INTEGER", nullable: false),
                    IdBlokady = table.Column<int>(type: "INTEGER", nullable: true),
                    Powod = table.Column<string>(type: "TEXT", nullable: false),
                    Data = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    Typ = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    IdZglaszanego = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Zgloszenia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Zgloszenia_Blokady_IdBlokady",
                        column: x => x.IdBlokady,
                        principalTable: "Blokady",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Zgloszenia_Users_IdZglaszajacego",
                        column: x => x.IdZglaszajacego,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Zgloszenia_Users_IdZglaszanego",
                        column: x => x.IdZglaszanego,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ListyZakupow",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlanPosilkowId = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListyZakupow", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ListyZakupow_MealPlans_PlanPosilkowId",
                        column: x => x.PlanPosilkowId,
                        principalTable: "MealPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlanPosilkowPrzepisy",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlanPosilkowId = table.Column<int>(type: "INTEGER", nullable: false),
                    PrzepisId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanPosilkowPrzepisy", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanPosilkowPrzepisy_MealPlans_PlanPosilkowId",
                        column: x => x.PlanPosilkowId,
                        principalTable: "MealPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlanPosilkowPrzepisy_Recipes_PrzepisId",
                        column: x => x.PrzepisId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Admins_UzytkownikId",
                table: "Admins",
                column: "UzytkownikId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Blokady_AdminId",
                table: "Blokady",
                column: "AdminId");

            migrationBuilder.CreateIndex(
                name: "IX_Blokady_UzytkownikId",
                table: "Blokady",
                column: "UzytkownikId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomFoods_UserId",
                table: "CustomFoods",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DietetykRatings_DietetykId_UserId",
                table: "DietetykRatings",
                columns: new[] { "DietetykId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DietetykRatings_UserId",
                table: "DietetykRatings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ListyZakupow_PlanPosilkowId",
                table: "ListyZakupow",
                column: "PlanPosilkowId");

            migrationBuilder.CreateIndex(
                name: "IX_MealPlans_RecipeId",
                table: "MealPlans",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_MealPlans_UserId",
                table: "MealPlans",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_NawykiWPlanie_NawykId",
                table: "NawykiWPlanie",
                column: "NawykId");

            migrationBuilder.CreateIndex(
                name: "IX_NawykiWPlanie_UzytkownikId",
                table: "NawykiWPlanie",
                column: "UzytkownikId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanPosilkowPrzepisy_PlanPosilkowId",
                table: "PlanPosilkowPrzepisy",
                column: "PlanPosilkowId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanPosilkowPrzepisy_PrzepisId",
                table: "PlanPosilkowPrzepisy",
                column: "PrzepisId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_IngredientId",
                table: "RecipeIngredients",
                column: "IngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_RecipeId",
                table: "RecipeIngredients",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeRatings_RecipeId_UserId",
                table: "RecipeRatings",
                columns: new[] { "RecipeId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecipeRatings_UserId",
                table: "RecipeRatings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_UserId",
                table: "Recipes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingLists_IngredientId",
                table: "ShoppingLists",
                column: "IngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingLists_UserId",
                table: "ShoppingLists",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Statystyki_UzytkownikId",
                table: "Statystyki",
                column: "UzytkownikId");

            migrationBuilder.CreateIndex(
                name: "IX_ToDos_UserId",
                table: "ToDos",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_DietetykId",
                table: "Users",
                column: "DietetykId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Zgloszenia_IdBlokady",
                table: "Zgloszenia",
                column: "IdBlokady");

            migrationBuilder.CreateIndex(
                name: "IX_Zgloszenia_IdZglaszajacego",
                table: "Zgloszenia",
                column: "IdZglaszajacego");

            migrationBuilder.CreateIndex(
                name: "IX_Zgloszenia_IdZglaszanego",
                table: "Zgloszenia",
                column: "IdZglaszanego");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomFoods");

            migrationBuilder.DropTable(
                name: "DietetykRatings");

            migrationBuilder.DropTable(
                name: "ListyZakupow");

            migrationBuilder.DropTable(
                name: "NawykiWPlanie");

            migrationBuilder.DropTable(
                name: "PlanPosilkowPrzepisy");

            migrationBuilder.DropTable(
                name: "RecipeIngredients");

            migrationBuilder.DropTable(
                name: "RecipeRatings");

            migrationBuilder.DropTable(
                name: "ShoppingLists");

            migrationBuilder.DropTable(
                name: "Statystyki");

            migrationBuilder.DropTable(
                name: "ToDos");

            migrationBuilder.DropTable(
                name: "Zgloszenia");

            migrationBuilder.DropTable(
                name: "Nawyki");

            migrationBuilder.DropTable(
                name: "MealPlans");

            migrationBuilder.DropTable(
                name: "Ingredients");

            migrationBuilder.DropTable(
                name: "Blokady");

            migrationBuilder.DropTable(
                name: "Recipes");

            migrationBuilder.DropTable(
                name: "Admins");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Dietetycy");
        }
    }
}
