using FluentValidation;
using FluentValidation.AspNetCore;
using IPOWeb;
using IPOWeb.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using IPOWeb.Extensions;

var builder = WebApplication.CreateBuilder(args);
var sessionTimeout = builder.Configuration.GetValue<int>(
    "Appsettings:SessionSettings:SessionTimeoutMinutes");

var authTimeout = builder.Configuration.GetValue<int>(
    "Appsettings:SessionSettings:AuthCookieTimeoutMinutes");

builder.WebHost.ConfigureKestrel(options =>
{
    // Set max request body size (500 MB)
    options.Limits.MaxRequestBodySize = 524288000;
});
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 524288000;
});
// ---------------- MVC ----------------

// 1. Add services to the container
builder.Services.AddDistributedMemoryCache(); // Required for Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(sessionTimeout);
    options.Cookie.Name = "IPO.Session";
    //options.IdleTimeout = TimeSpan.FromMinutes(10); // Session timeout
    //  options.IdleTimeout = TimeSpan.FromHours(12);
    options.Cookie.HttpOnly = true;                // Security: prevent JS access
    options.Cookie.IsEssential = true;             // Required for GDPR/compliance
   // options.Cookie.Name = ".STA.Session";

});
// Register AuditFilter so it can be applied globally and still use DI
builder.Services.AddScoped<IPOWeb.Filters.AuditFilter>();
builder.Services.AddControllersWithViews()
    .AddFluentValidation(fv =>
    {
        fv.RegisterValidatorsFromAssemblyContaining<UserManagementModelValidator>();
    })
        .AddMvcOptions(options => options.Filters.AddService<IPOWeb.Filters.AuditFilter>());
// Register Razor Pages without re-adding the audit filter (it's already added to MVC options above).
builder.Services.AddRazorPages();
// Configure audit logging options from configuration (appsettings.json) under "AuditLogging"
builder.Services.Configure<IPOWeb.Models.AuditLoggingOptions>(builder.Configuration.GetSection("AuditLogging"));

builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
// Register audit services (ApiAuditStore) and middleware to capture correlation id
builder.Services.AddAuditLogging();


builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login/Login";
        options.AccessDeniedPath = "/Login/Login";
        options.Cookie.Name = "IPO.Auth";
        options.Cookie.SecurePolicy = CookieSecurePolicy.None; // localhost
        //options.AccessDeniedPath = "/Login/Denied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(authTimeout);
        // options.ExpireTimeSpan = TimeSpan.FromHours(12);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
//builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();
//builder.Services.AddScoped<UserProvider>();


// Forwarded Headers

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IClientInfoService, ClientInfoService>();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;

    // If your application is behind a known proxy/load balancer,
    // you can add its IP here for stronger security.
    // options.KnownProxies.Add(IPAddress.Parse("127.0.0.1"));
});

// Generic bulk-mail plugin (Brevo)
builder.Services.Configure<IPOWeb.Models.BrevoOptions>(
    builder.Configuration.GetSection("Brevo"));

builder.Services.PostConfigure<IPOWeb.Models.BrevoOptions>(o =>
    o.ApiKey = Environment.GetEnvironmentVariable("BREVO_API_KEY"));

builder.Services.AddScoped<
    IPOWeb.Services.IBrevoMailService,
    IPOWeb.Services.BrevoMailService>();

// IPO Data Gateway required by MailingController
builder.Services.AddScoped<
    IPOWeb.Services.IIpoDataGatewayService,
    IPOWeb.Services.IpoDataGatewayService>();


var app = builder.Build();

// ---------------- PIPELINE ----------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseForwardedHeaders();
//app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
//app.UseMiddleware<ApiTokenRefreshMiddleware>();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Login}/{id?}");

app.Run();
