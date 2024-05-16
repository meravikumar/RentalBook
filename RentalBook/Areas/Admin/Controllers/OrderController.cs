using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RentalBook.DataAccess.Data;
using RentalBook.Models.Authentication;
using RentalBook.Models.ViewModels;

namespace RentalBook.Areas.Admin.Controllers
{
	[Authorize]
	public class OrderController : Controller
	{
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly RoleManager<IdentityRole> _roleManager;
		private readonly SignInManager<ApplicationUser> _signInManager;
		private readonly AppDbContext _db;

		[BindProperty]
		public OrderVM OrderVM { get; set; }
		public OrderController(UserManager<ApplicationUser> userManager,
			RoleManager<IdentityRole> roleManager, SignInManager<ApplicationUser> signInManager,
			IConfiguration configuration, AppDbContext db)
		{
			_userManager = userManager;
			_roleManager = roleManager;
			_signInManager = signInManager;
			_db = db;
		}

		public IActionResult Index(int pg = 1)
		{
			var UId = HttpContext.Session.GetString("UserId");
			var orderHeaders = (from o in _db.OrderHeaders
								join u in _db.Users on o.ApplicationUserId equals u.Id
								where o.ApplicationUserId == UId
								select new OrderVM
								{
									Id = o.Id,
									Name = o.Name,
									PhoneNumber = o.PhoneNumber,
									Email = u.Email,
									Status = o.OrderStatus,
									OrderTotal = o.OrderTotal,
									PaymentStatus = o.PaymentStatus
								}).ToList();
			if (orderHeaders.Count <= 0)
			{
				return View(orderHeaders);
			}
			//Paging
			const int pageSize = 3;
			if (pg < 1)
				pg = 1;
			int recsCount = orderHeaders.Count;
			var pager = new Pager(recsCount, pg, pageSize);
			int recSkip = (pg - 1) * pageSize;
			var data = orderHeaders.Skip(recSkip).Take(pager.PageSize).ToList();
			ViewBag.Pager = pager;

			return View(data);
		}

		public IActionResult Detail(int orderId)
		{
			var UId = HttpContext.Session.GetString("UserId");

			OrderVM = new OrderVM()
			{
				OrderDetails = _db.OrderDetails.Where(u => u.OrderHeaderId == orderId).ToList(),
				OrderHeader = _db.OrderHeaders.Where(a => a.Id == orderId).First(),
				Product = new(),
				ApplicationUser = _db.Users.Where(a => a.Id == UId).First()

			};

			return View(OrderVM);

		}
	}
}
