using Microsoft.EntityFrameworkCore;
using ObjetosIngresos.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<SistemaIngresoContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("con")));
// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();
 
app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
