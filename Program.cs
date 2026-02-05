using Microsoft.EntityFrameworkCore;
using BillingSoftware.Data;
using BillingSoftware.Services;
using QuestPDF.Infrastructure;

// Configure QuestPDF License (Community license for free usage)
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register Export Service
builder.Services.AddScoped<IExportService, ExportService>();

// Configure SQLite
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Data Source=BillingSoftware.db"));

var app = builder.Build();


// Ensure database is created and migrated
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated();

    // Migration to add Currency and ConversionRate columns
    try 
    {
        // Try adding the columns. If they exist, this will fail silently/be caught.
        // SQLite doesn't support "IF NOT EXISTS" in ADD COLUMN standardly across all versions easily without checks,
        // so we'll try to add them and ignore the specific error or just let it fail if present.
        // However, to be cleaner, we can check. But try-catch is robust enough for this context.
        context.Database.ExecuteSqlRaw("ALTER TABLE Clients ADD COLUMN Currency TEXT DEFAULT 'INR'");
        context.Database.ExecuteSqlRaw("ALTER TABLE Clients ADD COLUMN ConversionRate TEXT DEFAULT '1'"); // SQLite stores decimal as TEXT/REAL often, explicit type helps.
    }
    catch { /* Columns likely exist */ }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
