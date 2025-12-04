using BistroRater.Components;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
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
app.MapGet("/signin", async context => {
    await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties { RedirectUri = "/" });
});

AddDevEnvironmentUser(app);
app.UseAuthentication();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .DisableAntiforgery()
    .AddInteractiveServerRenderMode();
app.MapRazorPages();

app.Run();

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
         options.Authority = builder.Configuration["Auth:Authority"];
         options.ClientId = builder.Configuration["Auth:ClientId"];
         options.ClientSecret = builder.Configuration["Auth:ClientSecret"];
         if (options.Authority == null || options.ClientId == null || options.ClientSecret == null)
         {
             throw new MissingFieldException("Authentication: OAuth is not configured.");
         }

         options.ResponseType = "code";
         options.SaveTokens = true;

         options.Scope.Add("openid");
         options.Scope.Add("profile");
         options.Scope.Add("email");

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
    if (!requireAuth)
    {
        app.Use(async (context, next) =>
        {
            // Only generate fake identity if system has no authenticated user
            if (context.User?.Identity?.IsAuthenticated != true)
            {
                var identity = new ClaimsIdentity("DevAuth");
                identity.AddClaim(new Claim("sub", "dev-user"));
                identity.AddClaim(new Claim(ClaimTypes.Name, "Developer"));
                identity.AddClaim(new Claim(ClaimTypes.Email, "dev@example.com"));
                identity.AddClaim(new Claim(ClaimTypes.Role, "Developer"));

                context.User = new ClaimsPrincipal(identity);
            }

            await next();
        });
    }
}