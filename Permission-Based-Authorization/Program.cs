using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Permission_Based_Authorization.Contexts.UserManagement;
using Permission_Based_Authorization.Providers;
using Permission_Based_Authorization.Repositories;
using Permission_Based_Authorization.Seeds;
using Permission_Based_Authorization.Services;

var builder = WebApplication.CreateBuilder(args);

#region Register web services
builder.Services.AddControllersWithViews();
builder.Services.AddMvc().AddControllersAsServices();
#endregion

#region Register DbContext
builder.Services.AddDbContext<UserManagementDbContext>(options =>
               options.UseSqlServer(builder.Configuration.GetConnectionString("UserManagementDb")));
#endregion

#region Register our services & repositories
builder.Services
    .AddTransient<IHasherService, HasherService>()
    .AddTransient<IUserRepository, UserRepository>()
    .AddTransient<IRoleRepository, RoleRepository>()
    .AddTransient<IAuthenticationRepository, AuthenticationRepository>();
#endregion

#region Configure cookie authentication services
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignOutScheme = CookieAuthenticationDefaults.AuthenticationScheme;
}).AddCookie(
    CookieAuthenticationDefaults.AuthenticationScheme, (options) =>
    {
        options.Cookie.Name = "PermissionBasedAuthorization.Cookie";
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
        options.LoginPath = "/account/login";
        options.LogoutPath = "/account/logout";
        options.AccessDeniedPath = "/account/access-denied";
        options.ReturnUrlParameter = "redirect";
        options.SlidingExpiration = false;
    });
#endregion

#region A little further – setting up a cookie policy
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => true;
    options.MinimumSameSitePolicy = SameSiteMode.None;
});
#endregion

#region Configure policy & authorization provider
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionHandler>();
#endregion

var app = builder.Build();

#region Initialize Default Account
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var ctx = services.GetRequiredService<UserManagementDbContext>();
    var hasher = services.GetRequiredService<IHasherService>();
    DefaultAccount.Seed(ctx, hasher);
}
#endregion

#region Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days.
    // You may want to change this for production scenarios,
    // see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
#endregion

//++++++++ +1 Added cookie policy ++++++++//
app.UseCookiePolicy();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

//++++++++ Add authentication & authorization ++++++++//
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Products}/{action=Index}/{id?}");

app.Run();
