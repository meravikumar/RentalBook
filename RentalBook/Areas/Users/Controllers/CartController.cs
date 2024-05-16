using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RentalBook.DataAccess.Data;
using RentalBook.Models.Authentication;
using RentalBook.Models.Models;
using RentalBook.Models.ViewModels;
using RentalBook.Utility;
using Stripe.Checkout;

namespace RentalBook.Areas.Users.Controllers
{
	[Authorize]
	public class CartController : Controller
	{
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly RoleManager<IdentityRole> _roleManager;
		private readonly SignInManager<ApplicationUser> _signInManager;
		private readonly AppDbContext _db;

		[BindProperty]
		public CartVM CartVM { get; set; }
		public CartController(UserManager<ApplicationUser> userManager,
			RoleManager<IdentityRole> roleManager, SignInManager<ApplicationUser> signInManager,
			IConfiguration configuration, AppDbContext db)
		{
			_userManager = userManager;
			_roleManager = roleManager;
			_signInManager = signInManager;
			_db = db;
		}

		public IActionResult Index()
		{
			var UId = HttpContext.Session.GetString("UserId");

			var cartData = (from s in _db.ShoppingCarts
							join p in _db.Products on s.ProductId equals p.Id
							where s.UserId == UId
							select new CartVM
							{
								Name = p.Title,
								Description = p.Description,
								Price = s.Price,
								Quantity = s.Quantity,
								ImageUrl = p.ImageUrl,
								UserId = s.UserId,
								ProductId = s.ProductId,
								Id = s.Id,
								RentDuration = s.RentDuration,
							}).ToList();
			double P = 0;
			foreach (var item in cartData)
			{
				double TP = (item.Price * item.Quantity) * item.RentDuration;
				P += TP;
				ViewBag.OrderTotal = P;
			}
			return View(cartData);
		}

		public IActionResult Plus(int cartId)
		{
			var cartItem = _db.ShoppingCarts.FirstOrDefault(u => u.Id == cartId);
			var item = _db.Products.FirstOrDefault(p => p.Id == cartItem.ProductId);
			cartItem.Quantity++;
			int itemAfter = cartItem.Quantity;
			if (item.Quantity < itemAfter)
			{
				TempData["error"] = "Not enough item to add in your cart!";
				return RedirectToAction(nameof(Index));
			}
			else
			{
				_db.ShoppingCarts.Update(cartItem);
				_db.SaveChanges();

				var cartQty = _db.ShoppingCarts.Where(u => u.UserId == HttpContext.Session.GetString("UserId")).ToList();
				int qty = 0;
				foreach (var obj in cartQty)
				{
					qty += obj.Quantity;
				}
				HttpContext.Session.SetInt32("Quantity", qty);
				return RedirectToAction(nameof(Index));
			}
		}

		public IActionResult Minus(int cartId)
		{
			var cartItem = _db.ShoppingCarts.FirstOrDefault(u => u.Id == cartId);
			if (cartItem.Quantity <= 1)
			{
				_db.ShoppingCarts.Remove(cartItem);
			}
			else
			{
				cartItem.Quantity--;
				_db.ShoppingCarts.Update(cartItem);
			}
			_db.SaveChanges();

			var cartQty = _db.ShoppingCarts.Where(u => u.UserId == HttpContext.Session.GetString("UserId")).ToList();
			int qty = 0;
			foreach (var item in cartQty)
			{
				qty += item.Quantity;
			}
			HttpContext.Session.SetInt32("Quantity", qty);
			return RedirectToAction(nameof(Index));
		}

		public IActionResult Remove(int cartId)
		{
			var cartItem = _db.ShoppingCarts.FirstOrDefault(u => u.Id == cartId);
			_db.ShoppingCarts.Remove(cartItem);
			_db.SaveChanges();

			var cartQty = _db.ShoppingCarts.Where(u => u.UserId == HttpContext.Session.GetString("UserId")).ToList();
			int qty = 0;
			foreach (var item in cartQty)
			{
				qty += item.Quantity;
			}
			HttpContext.Session.SetInt32("Quantity", qty);
			return RedirectToAction(nameof(Index));
		}

		public IActionResult RentPeriodPlus(int cartId)
		{
			var cartItem = _db.ShoppingCarts.FirstOrDefault(u => u.Id == cartId);
			cartItem.RentDuration++;
			if (cartItem.RentDuration > 6)
			{
				TempData["error"] = "Can not order for more then 6 month!";
				return RedirectToAction(nameof(Index));
			}
			else
			{
				_db.ShoppingCarts.Update(cartItem);
				_db.SaveChanges();
				return RedirectToAction(nameof(Index));
			}
		}

		public IActionResult RentPeriodMinus(int cartId)
		{
			var cartItem = _db.ShoppingCarts.FirstOrDefault(u => u.Id == cartId);
			if (cartItem.RentDuration <= 1)
			{
				TempData["error"] = "Rent Period Can not be less then one month!";
				return RedirectToAction(nameof(Index));
			}
			else
			{
				cartItem.RentDuration--;
				_db.ShoppingCarts.Update(cartItem);
			}
			_db.SaveChanges();
			return RedirectToAction(nameof(Index));
		}

		public IActionResult Summary()
		{
			var UId = HttpContext.Session.GetString("UserId");

			CartVM = new CartVM()
			{
				CartItem = _db.ShoppingCarts.Where(u => u.UserId == UId).ToList(),
				Product = new(),
				OrderHeader = new(),
			};
			CartVM.OrderHeader.ApplicationUser = _db.Users.FirstOrDefault(u => u.Id == UId);
			CartVM.OrderHeader.Name = CartVM.OrderHeader.ApplicationUser.UserName;
			CartVM.OrderHeader.PhoneNumber = CartVM.OrderHeader.ApplicationUser.PhoneNumber;
			CartVM.OrderHeader.Area = CartVM.OrderHeader.ApplicationUser.Area;
			CartVM.OrderHeader.City = CartVM.OrderHeader.ApplicationUser.City;
			CartVM.OrderHeader.State = CartVM.OrderHeader.ApplicationUser.State;
			CartVM.OrderHeader.PostalCode = CartVM.OrderHeader.ApplicationUser.PinCode;

			ViewBag.cartData = (from s in _db.ShoppingCarts
								join p in _db.Products on s.ProductId equals p.Id
								where s.UserId == UId
								select new CartVM
								{
									Name = p.Title,
									Price = s.Price,
									Quantity = s.Quantity,
									RentDuration = s.RentDuration,
								}).ToList();
			double P = 0;
			foreach (var item in CartVM.CartItem)
			{
				double TP = (item.Price * item.Quantity) * item.RentDuration;
				P += TP;
				ViewBag.OrderTotal = P;
			}
			return View(CartVM);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult SummaryPost(CartVM cv)
		{
			var UId = HttpContext.Session.GetString("UserId");

			CartVM.CartItem = _db.ShoppingCarts.Where(u => u.UserId == UId).ToList();

			CartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
			CartVM.OrderHeader.OrderStatus = SD.StatusPending;
			CartVM.OrderHeader.OrderDate = DateTime.Now;
			CartVM.OrderHeader.ApplicationUserId = UId;

			double P = 0;
			foreach (var item in CartVM.CartItem)
			{
				double TP = (item.Price * item.Quantity) * item.RentDuration;
				P += TP;
				CartVM.OrderHeader.OrderTotal = P;
			}
			_db.OrderHeaders.Add(CartVM.OrderHeader);
			_db.SaveChanges();

			foreach (var item in CartVM.CartItem)
			{

				OrderDetail orderDetail = new()
				{
					ProductId = item.ProductId,
					OrderHeaderId = CartVM.OrderHeader.Id,
					Price = item.Price,
					Quantity = item.Quantity,
					RentDuration = item.RentDuration
				};
				_db.OrderDetails.Add(orderDetail);
				_db.SaveChanges();
			}


			//Stripe Settings
			var domain = "https://localhost:44321/";
			var options = new SessionCreateOptions
			{
				LineItems = new List<SessionLineItemOptions>(),
				Mode = "payment",
				SuccessUrl = domain + $"Users/Cart/OrderConfirmation?id={CartVM.OrderHeader.Id}",
				CancelUrl = domain + $"Users/Cart/Index",
			};
			foreach (var item in CartVM.CartItem)
			{
				var product = _db.Products.First(u => u.Id == item.ProductId);
				if (product != null)
				{
					int qty = product.Quantity - item.Quantity;
					product.IsOrdered = true;
					product.Quantity = qty;
					_db.Products.Update(product);
					_db.SaveChanges();
				}
				var sessionLineItem = new SessionLineItemOptions
				{
					PriceData = new SessionLineItemPriceDataOptions
					{
						UnitAmount = (long)((item.Price * item.RentDuration) * 100), //20.00 -> 2000
						Currency = "inr",
						ProductData = new SessionLineItemPriceDataProductDataOptions
						{
							Name = product.Title,
						},
					},
					Quantity = item.Quantity,
				};
				options.LineItems.Add(sessionLineItem);
			}


			var service = new SessionService();
			Session session = service.Create(options);
			CartVM.OrderHeader.SessionId = session.Id;
			CartVM.OrderHeader.PaymentIntentId = session.PaymentIntentId;
			_db.OrderHeaders.Update(CartVM.OrderHeader);
			_db.SaveChanges();

			Response.Headers.Add("Location", session.Url);
			return new StatusCodeResult(303);
		}

		public IActionResult OrderConfirmation(int id)
		{
			OrderHeader orderHeader = _db.OrderHeaders.FirstOrDefault(x => x.Id == id);
			var service = new SessionService();
			Session session = service.Get(orderHeader.SessionId);

			//check Stripe status
			if (session.PaymentStatus.ToLower() == "paid")
			{
				orderHeader.OrderStatus = SD.StatusApproved;
				orderHeader.PaymentStatus = SD.PaymentStatusApproved;
				orderHeader.PaymentDate = DateTime.Now;

				_db.OrderHeaders.Update(orderHeader);
				_db.SaveChanges();
			}
			List<ShoppingCart> cartList = _db.ShoppingCarts.Where(x => x.UserId == orderHeader.ApplicationUserId).ToList();
			_db.ShoppingCarts.RemoveRange(cartList);
			_db.SaveChanges();

			var cartQty = _db.ShoppingCarts.Where(u => u.UserId == orderHeader.ApplicationUserId).ToList();
			int qty = 0;
			foreach (var item in cartQty)
			{
				qty += item.Quantity;
			}
			HttpContext.Session.SetInt32("Quantity", qty);
			return View(id);
		}
	}
}
