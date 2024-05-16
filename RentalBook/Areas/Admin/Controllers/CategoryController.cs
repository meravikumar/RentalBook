using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RentalBook.DataAccess.Data;
using RentalBook.Models.Authentication;
using RentalBook.Models.EmailConfiguration;
using RentalBook.Models.Models;
using RentalBook.Models.ViewModels;

namespace RentalBook.Areas.Admin.Controllers
{
    public class CategoryController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _db;
        private readonly IEmailSender _emailSender;

        public CategoryController(UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager, SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration, AppDbContext db, IEmailSender emailSender)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _db = db;
            _emailSender = emailSender;
        }

        [Authorize(Roles = UserRoles.SuperAdmin)]
        public IActionResult Index(int pg = 1)
        {
            var category = _db.Categories.ToList();

            //Paging
            const int pageSize = 3;
            if (pg < 1)
                pg = 1;
            int recsCount = category.Count;
            var pager = new Pager(recsCount, pg, pageSize);
            int recSkip = (pg - 1) * pageSize;
            var data = category.Skip(recSkip).Take(pager.PageSize).ToList();
            ViewBag.Pager = pager;
            return View(data);
        }

        [Authorize(Roles = UserRoles.SuperAdmin)]
        public IActionResult AddCategory()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = UserRoles.SuperAdmin)]
        public IActionResult AddCategory(Category category)
        {
            _db.Categories.Add(category);
            _db.SaveChanges();
            return RedirectToAction("Index", "Category");
        }


        #region API CALLS
        [HttpDelete]
        [Authorize(Roles = UserRoles.SuperAdmin)]
        public IActionResult RemoveCategory(int? id)
        {
            var obj = _db.Categories.FirstOrDefault(u => u.Id == id);
            if (obj == null)
            {
                return NotFound();
            }
            var obj1 = _db.Products.FirstOrDefault(u => u.CategoryId == id);
            if (obj1 != null)
            {
                TempData["error"] = "Role is assigned can not be deleted!";
                return Json(new { success = false });
            }
            _db.Categories.Remove(obj);
            _db.SaveChanges();

            return Json(new { success = true });
        }
        #endregion

    }
}
