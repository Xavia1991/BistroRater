using BistroRater.Components;
using Database;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddRazorPages();

AddAuthentication(builder);
builder.Services.AddHttpContextAccessor();
builder.Services.AddAntiforgery();

builder.Services.AddHttpClient("ApiClient", (sp, client) =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var baseUrl = configuration["Api:BaseUrl"];

    if (string.IsNullOrWhiteSpace(baseUrl))
        throw new InvalidOperationException("API BaseUrl is not configured.");

    client.BaseAddress = new Uri(baseUrl);

}).AddHttpMessageHandler<ApiAuthorizationMessageHandler>();
builder.Services.AddTransient<ApiAuthorizationMessageHandler>();

builder.Services.AddDbContext<BistroContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default"));
});

var app = builder.Build();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseRouting();
app.MapGet("/signin", async context =>
{
    await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties { RedirectUri = "/" });
});
app.MapGet("/logout", async context =>
{
    // Löscht das lokale Cookie
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    context.Response.Redirect("/");
}
});
AddDevEnvironmentUser(app);
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .DisableAntiforgery()
    .AddInteractiveServerRenderMode();
app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BistroContext>();
    db.Database.Migrate();
}
app.MapControllers();

app.Run();



#region helpers
void AddAuthentication(WebApplicationBuilder builder)
{
    builder.Services
     .AddAuthentication(options =>
     {
         options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
         options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
     })
     .AddCookie(options =>
     {
         options.Cookie.HttpOnly = true;
         options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None;
         options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
     })
     .AddOpenIdConnect(options =>
     {
         var authority = builder.Configuration["Auth:Authority"];
         var clientId = builder.Configuration["Auth:ClientId"];
         var clientSecret = builder.Configuration["Auth:ClientSecret"];
         var audience = builder.Configuration["Auth:Audience"];

         if (authority == null || clientId == null || clientSecret == null)
         {
             throw new MissingFieldException("Authentication: OAuth is not configured.");
         }

         options.Authority = authority;
         options.ClientId = clientId;
         options.ClientSecret = clientSecret;

         options.ResponseType = "code";
         options.SaveTokens = true;

         options.Scope.Add("openid");
         options.Scope.Add("profile");
         options.Scope.Add("email");

         if (!string.IsNullOrWhiteSpace(audience))
         {
             options.Events = new OpenIdConnectEvents
             {
                 OnRedirectToIdentityProvider = context =>
                 {
                     context.ProtocolMessage.SetParameter("audience", audience);
                     return Task.CompletedTask;
                 }
             };
         }

         options.TokenValidationParameters = new TokenValidationParameters
         {
             NameClaimType = "name",
             RoleClaimType = "roles"
         };
     })
     .AddJwtBearer(options =>
     {
         options.Authority = builder.Configuration["Auth:Authority"];
         options.Audience = builder.Configuration["Auth:Audience"];

         options.TokenValidationParameters = new TokenValidationParameters
         {
             NameClaimType = "name",
             RoleClaimType = "roles"
         };
     });
}

void AddDevEnvironmentUser(WebApplication app)
{
    var requireAuth = builder.Configuration.GetValue<bool>("Auth:RequireAuth");

    // --- DEV MODE: fake user ---
    if (!requireAuth && app.Environment.IsDevelopment())
    {
        app.MapGet("/dev-login", async context =>
        {
            var identity = new ClaimsIdentity("DevAuth");
            identity.AddClaim(new Claim("sub", "dev-user"));
            identity.AddClaim(new Claim(ClaimTypes.Name, "Developer"));
            identity.AddClaim(new Claim(ClaimTypes.Email, "dev@example.com"));
            identity.AddClaim(new Claim(ClaimTypes.Role, "Developer"));

            var principal = new ClaimsPrincipal(identity);

            await context.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal);

            context.Response.Redirect("/", true);
        });
    }
}

#endregion