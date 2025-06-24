using Frontend.Handlers;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddRazorPages();

builder.Services.AddTransient<AccessTokenHandler>();
builder.Services.AddTransient<RefreshTokenHandler>();
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient("BackendApi", client =>
{
    client.BaseAddress = new Uri("https://localhost:7200");
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler();
    handler.UseCookies = true;
    handler.CookieContainer = new CookieContainer();
    handler.UseDefaultCredentials = false;
    handler.AllowAutoRedirect = true;
    return handler;
})
.AddHttpMessageHandler<RefreshTokenHandler>()
.AddHttpMessageHandler<AccessTokenHandler>();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "Bearer"; 
    options.DefaultChallengeScheme = "Bearer";    
})
.AddJwtBearer("Bearer", options =>
{
    options.Authority = "https://localhost:7200";
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true
    };
})
.AddCookie("MyCookieAuth", options =>
{
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.LoginPath = "/Login";
    options.AccessDeniedPath = "/AccessDenied";
});
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();


app.MapRazorPages();

app.Run();