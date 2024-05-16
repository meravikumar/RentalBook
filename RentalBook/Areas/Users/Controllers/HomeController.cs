using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Org.BouncyCastle.Crypto;
using RentalBook.DataAccess.Data;
using RentalBook.Models.Authentication;
using RentalBook.Models.EmailConfiguration;
using RentalBook.Models.Models;
using RentalBook.Models.ViewModels;
using System.Diagnostics;

namespace RentalBook.Areas.Users.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _db;
        private readonly IEmailSender _emailSender;

        public HomeController(UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager, SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration, AppDbContext db, IEmailSender emailSender,
            ILogger<HomeController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _db = db;
            _emailSender = emailSender;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var productList = _db.Products.Where(u => u.IsActive == true).ToList();
            return View(productList);
        }

        public IActionResult Details(int productId)
        {
            HttpContext.Session.SetString("ProductId", productId.ToString());
            ShoppingCart cartObj = new()
            {
                Quantity = 1,
                ProductId = productId,
                Product = _db.Products.FirstOrDefault(u => u.Id == productId),
                Discount = _db.Discounts.FirstOrDefault(a => a.ProductId == productId),
            };

            return View(cartObj);
        }

        [HttpPost]
        [Authorize]
        public IActionResult Details(ShoppingCart shoppingCart)
        {
            var UId = HttpContext.Session.GetString("UserId");

            shoppingCart.UserId = UId;

            ShoppingCart existingItem = _db.ShoppingCarts.FirstOrDefault(u => u.UserId == UId &&
                                                                              u.ProductId == shoppingCart.ProductId);
            if (existingItem == null)
            {
                _db.ShoppingCarts.Add(shoppingCart);
            }
            else
            {
                existingItem.Quantity = shoppingCart.Quantity;
                _db.ShoppingCarts.Update(existingItem);
            }
            _db.SaveChanges();

            var cartQty = _db.ShoppingCarts.Where(u => u.UserId == UId).ToList();
            int qty = 0;
            foreach (var item in cartQty)
            {
                qty += item.Quantity;
            }
            HttpContext.Session.SetInt32("Quantity", qty);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Profile()
        {
            return View();
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            if (User?.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            else
            {
                return View();
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(Login model, string? returnUrl)
        {
            if (ModelState.IsValid)
            {
                ApplicationUser user = await _userManager.FindByNameAsync(model.Username);
                if (user != null)
                {
                    var isValid = await _signInManager.PasswordSignInAsync(model.Username, model.Password, false, false);
                    if (isValid.Succeeded)
                    {
                        //Get Roles
                        var userRoles = _userManager.GetRolesAsync(user).Result.First();

                       
						var cartQty = _db.ShoppingCarts.Where(u => u.UserId == user.Id).ToList();
                        int qty = 0;
                        foreach (var item in cartQty)
                        {
                            qty += item.Quantity;
                        }
						HttpContext.Session.SetInt32("Quantity", qty);

						if (!string.IsNullOrEmpty(returnUrl))
                        {
                            if (userRoles.Contains("SuperAdmin"))
                            {
								HttpContext.Session.SetString("Username", model.Username);
								HttpContext.Session.SetString("UserId", user.Id);
								HttpContext.Session.SetString("Role", userRoles);
								return RedirectToAction("Dashboard", "User", new { area = "Admin" });
                            }
                            else if (userRoles.Contains("Admin"))
                            {
                                if (user.StatusTypes == StatusType.Approve)
                                {
									HttpContext.Session.SetString("Username", model.Username);
									HttpContext.Session.SetString("UserId", user.Id);
									HttpContext.Session.SetString("Role", userRoles);
									return RedirectToAction("Dashboard", "User", new { area = "Admin" });
                                }
                                else
                                {
                                    await _signInManager.SignOutAsync();
                                    TempData["error"] = "Status Is not Approved Yet";
                                    return View(model);
                                }
                            }
                            else if (userRoles.Contains("Librarian") || userRoles.Contains("Student") || userRoles.Contains("Dealer"))
                            {
                                if (user.StatusTypes == StatusType.Approve)
                                {
									HttpContext.Session.SetString("Username", model.Username);
									HttpContext.Session.SetString("UserId", user.Id);
									HttpContext.Session.SetString("Role", userRoles);
									return RedirectToAction("Dashboard", "User", new { area = "Admin" });
                                }
                                else
                                {
                                    await _signInManager.SignOutAsync();
                                    TempData["error"] = "Please wait for Approval Message. Your registration request is Pending!";
									return RedirectToAction("Index", "Home");
								}
                            }
                            else
                            {
								HttpContext.Session.SetString("Username", model.Username);
								HttpContext.Session.SetString("UserId", user.Id);
								HttpContext.Session.SetString("Role", userRoles);
								return LocalRedirect(returnUrl);
                            }
                        }
                        else
                        {
                            return RedirectToAction("Index", "Home");
                        }
                    }
                    TempData["error"] = "Invalid Password!";
                    return View(model);
                }
                TempData["error"] = "User Doesn't Exists!";
                return View(model);
            }
            // If we got this far, something failed, redisplay form
            return View(model);
        }


        //User Registration Dealer or Customer
        public IActionResult Register()
        {
            ViewBag.Roles = new SelectList(_db.Roles.Where(u => !u.Name.Contains("Admin") &&
                                                               !u.Name.Contains("SuperAdmin"))
                                                               .ToList(), "Name", "Name");
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Register(UserVM model)
        {
            var userExists = await _userManager.FindByEmailAsync(model.Email);
            if (userExists != null)
            {
                TempData["error"] = "User Already Exist! Use another Email.";
                ViewBag.Roles = new SelectList(_db.Roles.Where(u => !u.Name.Contains("Admin") &&
                                                               !u.Name.Contains("SuperAdmin"))
                                                               .ToList(), "Name", "Name");
                return View(model);
            }
            ApplicationUser user = new ApplicationUser()
            {
                Email = model.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = model.Username,
                PhoneNumber = model.PhoneNumber,
                Area = model.Area,
                City = model.City,
                State = model.State,
                PinCode = model.PinCode,
            };
            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                TempData["error"] = "User creation failed! Please check user details and try again!";
                return View(model);
            }
            else
            {
                if (model.Role == "Dealer")
                {
                    if (await _roleManager.RoleExistsAsync(UserRoles.Dealer))
                    {
                        await _userManager.AddToRoleAsync(user, UserRoles.Dealer);
                    }
                }
                else if (model.Role == "Librarian")
                {
                    if (await _roleManager.RoleExistsAsync(UserRoles.Librarian))
                    {
                        await _userManager.AddToRoleAsync(user, UserRoles.Librarian);
                    }
                }
                else if (model.Role == "Student")
                {
                    if (await _roleManager.RoleExistsAsync(UserRoles.Student))
                    {
                        await _userManager.AddToRoleAsync(user, UserRoles.Student);
                    }
                }
                else if (model.Role == "User")
                {
                    if (await _roleManager.RoleExistsAsync(UserRoles.Customer))
                    {
                        await _userManager.AddToRoleAsync(user, UserRoles.Customer);
                    }
                }
                return RedirectToAction("Login", "Home");
            }
        }

        public IActionResult ForgotPassword()
        {          
                return View();  
        }


        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordVM model)
        {
            var userExists = await _userManager.FindByEmailAsync(model.Email);
            if (userExists == null)
            {
                TempData["error"] = "User Doesn't Exist! Enter Correct Email.";
                return View(model);
            }
            ApplicationUser user = new ApplicationUser()
            {
                Email= model.Email,
                PasswordHash = model.Password
            };
            
            _db.Users.Update(user);
            await _db.SaveChangesAsync();
            TempData["success"] = "Password Updated Successfully!";
            return RedirectToAction("Login");
        }

        //User Logout section
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }


    }
}