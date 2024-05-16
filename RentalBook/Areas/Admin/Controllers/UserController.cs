using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalBook.DataAccess.Data;
using RentalBook.Models.Authentication;
using RentalBook.Models.EmailConfiguration;
using RentalBook.Models.ViewModels;

namespace RentalBook.Areas.Admin.Controllers
{

	public class UserController : Controller
	{

		private readonly UserManager<ApplicationUser> _userManager;
		private readonly RoleManager<IdentityRole> _roleManager;
		private readonly SignInManager<ApplicationUser> _signInManager;
		private readonly IConfiguration _configuration;
		private readonly AppDbContext _db;
		private readonly IEmailSender _emailSender;

		public UserController(UserManager<ApplicationUser> userManager,
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

		// Register Admin
		[Authorize(Roles = UserRoles.SuperAdmin)]
		public IActionResult RegisterAdmin()
		{
			return View();
		}
		[HttpPost]
		[Authorize(Roles = UserRoles.SuperAdmin)]
		public async Task<IActionResult> RegisterAdmin(UserVM model)
		{
			var userExists = await _userManager.FindByEmailAsync(model.Email);
			if (userExists != null)
			{
				TempData["error"] = "Admin Already Exist! Use another Email.";
				return View(model);
			}
			ApplicationUser user = new ApplicationUser()
			{
				Email = model.Email,
				SecurityStamp = Guid.NewGuid().ToString(),
				UserName = model.Username,
				PhoneNumber = model.PhoneNumber,
			};
            if (await _roleManager.RoleExistsAsync(UserRoles.Admin))
            {
				var result = await _userManager.CreateAsync(user, model.Password);
			if (!result.Succeeded)
			{
				TempData["error"] = "User creation failed! Please check user details and try again!";
				return View(model);
			}
                await _userManager.AddToRoleAsync(user, UserRoles.Admin);
            }
            else
            {
                TempData["error"] = "Role doesn't exist in the database! Please Add role.";
                return View(model);
            }
			return RedirectToAction("Admin", "User");
		}


		//SuperAdmin Dashboard
		[HttpGet]
		public IActionResult Dashboard()
		{
			if (User?.Identity?.IsAuthenticated == true)
			{
				if (HttpContext.Session.GetString("Role") == "User")
				{
					return RedirectToAction("Index", "Home", new { area = "Users" });
				}
				return View("Dashboard");
			}
			else
			{
				TempData["error"] = "Session ended! Login Again";
				return RedirectToAction("Login", "Home", new { area = "Users" });
			}

		}


		//Get all Role exists in the database and Add new Role
		[Authorize(Roles = UserRoles.SuperAdmin)]
		public IActionResult GetRole()
		{
			var roles = _roleManager.Roles.ToList();
			return View(roles);
		}
		[Authorize(Roles = UserRoles.SuperAdmin)]
		public IActionResult AddRole()
		{
			return View(new IdentityRole());
		}
		[HttpPost]
		[Authorize(Roles = UserRoles.SuperAdmin)]
		public async Task<IActionResult> AddRole(IdentityRole Role)
		{
			await _roleManager.CreateAsync(Role);
			return RedirectToAction("GetRole", "User");
		}

		//Remove Role
		#region API CALLS
		[HttpDelete]
		[Authorize(Roles = UserRoles.SuperAdmin)]
		public IActionResult RemoveRole(string? id)
		{
			var obj = _db.Roles.FirstOrDefault(u => u.Id == id);
			if (obj == null)
			{
				return NotFound();
			}
			var obj1 = _db.UserRoles.FirstOrDefault(u => u.RoleId == id);
			if (obj1 != null)
			{
				TempData["error"] = "Role is assigned can not be deleted!";
				return Json(new { success = false });
			}
			_db.Roles.Remove(obj);
			_db.SaveChanges();

			return Json(new { success = true });
		}
		#endregion


		// Admin List (Approve, reject, Block..)
		[Authorize(Roles = UserRoles.SuperAdmin)]
		public async Task<IActionResult> Admin(string? searchString, int pg = 1)
		{
			var temp = new List<UserVM>();
            temp = await _db.Users
                .Join(_db.UserRoles, u => u.Id, ur => ur.UserId, (u, ur) => new { User = u, ur.RoleId })
                .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.User, Role = r })
                .Where(ur => ur.Role.Name == "Admin")
                .Select(ur => new UserVM
                {
                    Username = ur.User.UserName,
                    Email = ur.User.Email,
                    PhoneNumber = ur.User.PhoneNumber,
                    StatusTypes = ur.User.StatusTypes.ToString(),
                    Reason = ur.User.Reason,
                    IsActive = ur.User.IsActive,
                    Role = ur.Role.Name
                }).ToListAsync();
            if (searchString == null)
			{
				temp = temp;
			}
			else
			{
				ViewBag.SearchStr = searchString;
				temp = temp.Where(u => u.Username.ToLower().Contains(searchString.ToLower())).ToList();
			}
			//Paging
			const int pageSize = 2;
			if (pg < 1)
				pg = 1;
			int recsCount = temp.Count;
			var pager = new Pager(recsCount, pg, pageSize);
			int recSkip = (pg - 1) * pageSize;
			var data = temp.Skip(recSkip).Take(pager.PageSize).ToList();
			ViewBag.Pager = pager;
			return View(data);
		}

		// Dealer List (Approve, reject, Block..)
		[Authorize(Roles = "SuperAdmin, Admin")]
		public IActionResult Dealer(string? searchString, int pg = 1)
		{
			var temp = new List<UserVM>();
            temp = (from u in _db.Users
                    join ur in _db.UserRoles on u.Id equals ur.UserId
                    join r in _db.Roles on ur.RoleId equals r.Id
                    where r.Name == "Dealer"
                    select new UserVM
                    {
                        Id = u.Id,
                        Username = u.UserName,
                        Email = u.Email,
                        PhoneNumber = u.PhoneNumber,
                        Area = u.Area,
                        City = u.City,
                        State = u.State,
                        PinCode = u.PinCode,
                        StatusTypes = u.StatusTypes.ToString(),
                        IsActive = u.IsActive,
                        Role = r.Name,
                        Reason = u.Reason,
                    }).ToList();

            if (searchString == null)
			{
				temp = temp;
			}
			else
			{
				ViewBag.SearchStr = searchString;
				temp = temp.Where(u => u.Username.ToLower().Contains(searchString.ToLower())).ToList();
			}

			//Paging
			const int pageSize = 3;
			if (pg < 1)
				pg = 1;
			int recsCount = temp.Count;
			var pager = new Pager(recsCount, pg, pageSize);
			int recSkip = (pg - 1) * pageSize;
			var data = temp.Skip(recSkip).Take(pager.PageSize).ToList();
			ViewBag.Pager = pager;
			return View(data);
		}

        // Librarian List (Approve, reject, Block..)
        [Authorize(Roles = "SuperAdmin, Admin")]
        public IActionResult Librarian(string? searchString, int pg = 1)
        {
            var temp = new List<UserVM>();
            temp = (from u in _db.Users
                    join ur in _db.UserRoles on u.Id equals ur.UserId
                    join r in _db.Roles on ur.RoleId equals r.Id
                    where r.Name == "Librarian"
                    select new UserVM
                    {
                        Id = u.Id,
                        Username = u.UserName,
                        Email = u.Email,
                        PhoneNumber = u.PhoneNumber,
                        Area = u.Area,
                        City = u.City,
                        State = u.State,
                        PinCode = u.PinCode,
                        StatusTypes = u.StatusTypes.ToString(),
                        IsActive = u.IsActive,
                        Role = r.Name,
                        Reason = u.Reason,
                    }).ToList();

            if (searchString == null)
            {
				temp = temp;
            }
            else
            {
                ViewBag.SearchStr = searchString;
                temp = temp.Where(u => u.Username.ToLower().Contains(searchString.ToLower())).ToList();
            }

            //Paging
            const int pageSize = 3;
            if (pg < 1)
                pg = 1;
            int recsCount = temp.Count;
            var pager = new Pager(recsCount, pg, pageSize);
            int recSkip = (pg - 1) * pageSize;
            var data = temp.Skip(recSkip).Take(pager.PageSize).ToList();
            ViewBag.Pager = pager;
            return View(data);
        }

        // Librarian List (Approve, reject, Block..)
        [Authorize(Roles = "SuperAdmin, Admin")]
        public IActionResult Student(string? searchString, int pg = 1)
        {
            var temp = new List<UserVM>();
            temp = (from u in _db.Users
                    join ur in _db.UserRoles on u.Id equals ur.UserId
                    join r in _db.Roles on ur.RoleId equals r.Id
                    where r.Name == "Student"
                    select new UserVM
                    {
                        Id = u.Id,
                        Username = u.UserName,
                        Email = u.Email,
                        PhoneNumber = u.PhoneNumber,
                        Area = u.Area,
                        City = u.City,
                        State = u.State,
                        PinCode = u.PinCode,
                        StatusTypes = u.StatusTypes.ToString(),
                        IsActive = u.IsActive,
                        Role = r.Name,
                        Reason = u.Reason,
                    }).ToList();

            if (searchString == null)
            {
				temp = temp;
            }
            else
            {
                ViewBag.SearchStr = searchString;
                temp = temp.Where(u => u.Username.ToLower().Contains(searchString.ToLower())).ToList();
            }

            //Paging
            const int pageSize = 3;
            if (pg < 1)
                pg = 1;
            int recsCount = temp.Count;
            var pager = new Pager(recsCount, pg, pageSize);
            int recSkip = (pg - 1) * pageSize;
            var data = temp.Skip(recSkip).Take(pager.PageSize).ToList();
            ViewBag.Pager = pager;
            return View(data);
        }


        //Dealer or Admin Approval, Reject, Block, Unblock 
        [Authorize(Roles = "SuperAdmin, Admin")]
		public async Task<IActionResult> Approve(string data)
		{
			var status = await _userManager.FindByNameAsync(data);
			if (status.StatusTypes == StatusType.Pending)
			{
				status.StatusTypes = StatusType.Approve;
				_db.Update(status);
				await _db.SaveChangesAsync();

				//Send an Email for approval
				var message =
					new EmailMessage(new string[] { status.Email }, "Approval Status", "Welcome, Your account request has been approved.");
				_emailSender.SendEmail(message);
			}
			var userRoles = await _userManager.GetRolesAsync(status);
			if (userRoles.Contains("Admin"))
			{
				TempData["success"] = "Admin Approved!";
				return RedirectToAction("Admin", "User");
			}
			else if(userRoles.Contains("Librarian"))
			{
                TempData["success"] = "Librarian Approved!";
                return RedirectToAction("Librarian", "User");
            }
			else if(userRoles.Contains("Student"))
			{
                TempData["success"] = "Student Approved!";
                return RedirectToAction("Student", "User");
            }
            TempData["success"] = "Dealer Approved!";
            return RedirectToAction("Dealer", "User");

        }
		[HttpPost]
		[Authorize(Roles = "SuperAdmin, Admin")]
		public async Task<IActionResult> Reject(string Username, string Reason)
		{
			var status = await _userManager.FindByNameAsync(Username);
			if (status.StatusTypes == StatusType.Pending)
			{
				if (Reason != null)
				{
					status.StatusTypes = StatusType.Rejected;
					status.Reason = Reason;
					_db.Update(status);
					await _db.SaveChangesAsync();

					//Send an Email for approval
					var message =
						new EmailMessage(new string[] { status.Email }, "Rejected Reason", Reason);
					_emailSender.SendEmail(message);
				}
				else
				{
					return View();
				}
			}
			var userRoles = await _userManager.GetRolesAsync(status);
			if (userRoles.Contains("Admin"))
			{
				TempData["success"] = "Admin Rejected!";
				return RedirectToAction("Admin", "User");
			}
            else if (userRoles.Contains("Librarian"))
            {
                TempData["success"] = "Librarian Approved!";
                return RedirectToAction("Librarian", "User");
            }
            else if (userRoles.Contains("Student"))
            {
                TempData["success"] = "Student Approved!";
                return RedirectToAction("Student", "User");
            }
            TempData["success"] = "Dealer Rejected!";
			return RedirectToAction("Dealer", "User");
		}

		[Authorize(Roles = "SuperAdmin, Admin")]
		public async Task<IActionResult> Block(string data)
		{
			var status = await _userManager.FindByNameAsync(data);
			if (status.StatusTypes == StatusType.Approve)
			{
				status.StatusTypes = StatusType.Block;
				_db.Update(status);
				await _db.SaveChangesAsync();
			}
			var userRoles = await _userManager.GetRolesAsync(status);
			if (userRoles.Contains("Admin"))
			{
				TempData["success"] = "Admin Blocked!";
				return RedirectToAction("Admin", "User");
			}
            else if (userRoles.Contains("Librarian"))
            {
                TempData["success"] = "Librarian Approved!";
                return RedirectToAction("Librarian", "User");
            }
            else if (userRoles.Contains("Student"))
            {
                TempData["success"] = "Student Approved!";
                return RedirectToAction("Student", "User");
            }
            TempData["success"] = "Dealer Blocked!";
			return RedirectToAction("Dealer", "User");
		}

		[Authorize(Roles = "SuperAdmin, Admin")]
		public async Task<IActionResult> UnBlock(string data)
		{
			var status = await _userManager.FindByNameAsync(data);
			if (status.StatusTypes == StatusType.Block)
			{
				status.StatusTypes = StatusType.Pending;
				_db.Update(status);
				await _db.SaveChangesAsync();
			}
			var userRoles = await _userManager.GetRolesAsync(status);
			if (userRoles.Contains("Admin"))
			{
				TempData["success"] = "Admin UnBlocked!";
				return RedirectToAction("Admin", "User");
			}
            else if (userRoles.Contains("Librarian"))
            {
                TempData["success"] = "Librarian Approved!";
                return RedirectToAction("Librarian", "User");
            }
            else if (userRoles.Contains("Student"))
            {
                TempData["success"] = "Student Approved!";
                return RedirectToAction("Student", "User");
            }
            TempData["success"] = "Dealer UnBlocked!";
			return RedirectToAction("Dealer", "User");
		}

	}
}
