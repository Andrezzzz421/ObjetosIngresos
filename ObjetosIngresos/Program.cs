using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ObjetosIngresos.Models;
using ObjetosIngresos.Services;
using System.Security.Claims;

System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

// 🟢 CAMBIO: Reemplazado UseSqlServer por UseNpgsql para conectar a Supabase
builder.Services.AddDbContext<SistemaIngresoContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("con"), npgsqlOptionsAction: npgsqlOptions =>
    { 
    }));

builder.Services.AddScoped<UsuarioServices>();
builder.Services.AddScoped<AuthServices>();
builder.Services.AddScoped<CatalogoServices>();
builder.Services.AddScoped<ElementoServices>();
var projectId = builder.Configuration["FirebaseConfig:ProjectId"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.Cookie.Name = "SistemaIngresoSesion";
    options.LoginPath = "/Auth/Login";
    options.AccessDeniedPath = "/";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Path = "/";
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromHours(1);
    options.SlidingExpiration = true;
})
.AddJwtBearer(options =>
{
    options.Authority = $"https://securetoken.google.com/{projectId}";
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = $"https://securetoken.google.com/{projectId}",
        ValidateAudience = true,
        ValidAudience = projectId,
        ValidateLifetime = true
    };
});

var rutaConfig = Path.Combine(Directory.GetCurrentDirectory(), "firebase-admin.json");
if (FirebaseApp.DefaultInstance == null)
{
    FirebaseApp.Create(new AppOptions()
    {
        Credential = GoogleCredential.FromFile(rutaConfig)
    });
}

builder.Services.AddControllersWithViews();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();