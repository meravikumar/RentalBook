using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RentalBook.DataAccess.Data;
using RentalBook.Models.Authentication;
using RentalBook.Models.EmailConfiguration;
using RentalBook.Models.ViewModels;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

//var connectionStrings = builder.Configuration.GetConnectionString("DefaultConnection");
//builder.Services.AddDbContext<AppDbContext>(options => options.UseMySql(connectionStrings,
//    ServerVersion.AutoDetect(connectionStrings)));

var connectionStrings = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionStrings));

//Stripe Payment
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));

// For Identity 
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = false)
                .AddEntityFrameworkStores<AppDbContext>();

//Email configuration
var emailConfig = builder.Configuration
        .GetSection("EmailConfiguration")
        .Get<EmailConfiguration>();
builder.Services.AddSingleton(emailConfig);

//Store Session
builder.Services.AddSession(option =>
{
    option.IdleTimeout = TimeSpan.FromMinutes(30);
    option.Cookie.HttpOnly = true;
    option.Cookie.IsEssential = true;
});

//Authorization
builder.Services.ConfigureApplicationCookie(options =>
{
    // Set the login URL for handling unauthorized access
    options.LoginPath = $"/Users/Home/Login";
    options.LogoutPath = $"/Users/Home/Logout";
    options.AccessDeniedPath = $"/Users/Home/Login";
    
});

//builder.Services.AddAuthorization(options =>
//{
//    options.DefaultPolicy = new AuthorizationPolicyBuilder()
//        .RequireAuthenticatedUser()
//        .Build();
//    options.AddPolicy("RequireAdminRole", policy =>
//        policy.RequireRole("Admin"));
//    options.AddPolicy("RequireDealerRole", policy =>
//        policy.RequireRole("Dealer"));
//    options.AddPolicy("RequireSuperAdminRole", policy =>
//        policy.RequireRole("SuperAdmin"));
//})
//.ConfigureApplicationCookie(options =>
//{
//	// Set the login URL for handling unauthorized access
//	options.LoginPath = $"/Users/Home/Login";
//    options.LogoutPath = $"/Users/Home/Logout";
//    options.AccessDeniedPath = $"/Users/Home/Login";
//});

builder.Services.AddScoped<IEmailSender, EmailSender>();

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

StripeConfiguration.ApiKey = builder.Configuration.GetSection("Stripe:SecretKey").Get<string>();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{area=Users}/{controller=Home}/{action=Index}/{id?}");

app.Run();