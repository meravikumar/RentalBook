using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using RentalBook.Models.Authentication;
using RentalBook.Models.ViewModels;
using RentalBook.DataAccess.Data;
using RentalBook.Models.Models;

namespace RentalBook.Areas.Admin.Controllers
{
	[Authorize]
	public class ProductController : Controller
	{
		private readonly AppDbContext _db;
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly IWebHostEnvironment _hostEnvironment;
		public ProductController(AppDbContext db, IWebHostEnvironment hostEnvironment,
			   UserManager<ApplicationUser> userManager)
		{
			_db = db;
			_hostEnvironment = hostEnvironment;
			_userManager = userManager;
		}

		// All product list
		[HttpGet]
		public IActionResult Index(string? searchString, string sortOrder, int pg = 1)
		{
			var UId = HttpContext.Session.GetString("UserId");
			var Role = HttpContext.Session.GetString("Role");
			var productData = new List<Product>();

			if (Role == "SuperAdmin" || Role == "Admin")
			{
				if (searchString == null)
				{
					productData = _db.Products.ToList();
				}
				else
				{
					ViewBag.SearchStr = searchString;
					productData = _db.Products.Where(u => u.Title.ToLower().Contains(searchString.ToLower()) ||
														  u.Renter.ToLower().Contains(searchString.ToLower())).ToList();
				}
			}
			else if (Role == "Dealer" || Role == "Librarian" || Role == "Student")
			{
				if (searchString == null)
				{
					productData = _db.Products.Where(u => u.UserId == UId).ToList();
				}
				else
				{
					ViewBag.SearchStr = searchString;
					productData = _db.Products.Where(u => u.Title.ToLower().Contains(searchString.ToLower()) ||
														   u.Renter.ToLower().Contains(searchString.ToLower()) &&
															  u.UserId == UId).ToList();
				}
			}
			//Sorting
			ViewData["NameOrder"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
			ViewData["PriceOrder"] = string.IsNullOrEmpty(sortOrder) ? "price_desc" : "";
			switch (sortOrder)
			{
				case "name_desc":
					productData = productData.OrderByDescending(a => a.Title).ToList();
					break;
				case "price_desc":
					productData = productData.OrderByDescending(a => a.Price).ToList();
					break;
				default:
					productData = productData.OrderBy(a => a.Title).ToList();
					break;
			}

			//Paging
			const int pageSize = 3;
			if (pg < 1)
				pg = 1;
			int recsCount = productData.Count;
			var pager = new Pager(recsCount, pg, pageSize);
			int recSkip = (pg - 1) * pageSize;
			var data = productData.Skip(recSkip).Take(pager.PageSize).ToList();
			ViewBag.Pager = pager;

			return View(data);
		}

		// Add or Edit product
		[Authorize(Roles = "Dealer, Librarian, Student")]
		public IActionResult Upsert(int? id)
		{
			ProductVM productVM = new()
			{
				Products = new(),
				UserId = HttpContext.Session.GetString("UserId"),
				UName = HttpContext.Session.GetString("Username"),
				CategoryList = _db.Categories.ToList().Select(
				i => new SelectListItem
				{
					Text = i.Name,
					Value = i.Id.ToString(),
				}),
			};
			if (id == null || id == 0)
			{
				//create product
				ViewBag.UserId = productVM.UserId;
				ViewBag.UName = productVM.UName;
				return View(productVM);
			}
			else
			{
				//update product
				ViewBag.UserId = productVM.UserId;
				ViewBag.UName = productVM.UName;
				productVM.Products = _db.Products.FirstOrDefault(u => u.Id == id);
				return View(productVM);
			}
		}
		[HttpPost]
		[ValidateAntiForgeryToken] //prevents from cross-site forgery attack
		[Authorize(Roles = "Dealer, Librarian, Student")]
		public IActionResult Upsert(ProductVM obj, IFormFile? file)
		{

			if (ModelState.IsValid)
			{
				string wwwRootPath = _hostEnvironment.WebRootPath;
				if (file != null)
				{
					string fileName = Guid.NewGuid().ToString();
					var uploads = Path.Combine(wwwRootPath, @"Images\products");
					var extension = Path.GetExtension(file.FileName);
					if (obj.Products.ImageUrl != null)
					{
						var oldImagePath = Path.Combine(wwwRootPath, obj.Products.ImageUrl.TrimStart('\\'));
						if (System.IO.File.Exists(oldImagePath))
						{
							System.IO.File.Delete(oldImagePath);
						}
					}
					using (var fileStreams = new FileStream(Path.Combine(uploads, fileName + extension), FileMode.Create))
					{
						file.CopyTo(fileStreams);
					}
					obj.Products.ImageUrl = @"\Images\products\" + fileName + extension;
				}

				if (obj.Products.Id == 0)
				{
					_db.Products.Add(obj.Products);
					TempData["success"] = "Product Added successfully";
				}
				else
				{
					_db.Products.Update(obj.Products);
					TempData["success"] = "Product Updated successfully";
				}
				_db.SaveChanges();


				return RedirectToAction("Index");
			}
			return View(obj);
		}


		// Activate or Deactivate product
		[Authorize(Roles = "SuperAdmin, Admin, Dealer, Librarian, Student")]
		public IActionResult IsActive(int? id)
		{
			var product = _db.Products.Find(id);
			if (product.IsActive == true)
			{
				product.IsActive = false;
			}
			else
			{
				product.IsActive = true;
			}
			_db.Products.Update(product);
			_db.SaveChanges();
			return RedirectToAction("Index", "Product");

		}


		// Add Discount for each product
		[Authorize(Roles = "Dealer, Librarian, Student")]
		public IActionResult Discount(int? id)
		{
			ViewBag.ProductId = id;
			var price = _db.Products.FirstOrDefault(u => u.Id == id);
			if (price != null)
			{
				ViewBag.Price = price.Price;
			}
			return View();

		}
		[HttpPost]
		[Authorize(Roles = "Dealer, Librarian, Student")]
		public IActionResult Discount(DiscountVM discountVM, DiscountModel dv)
		{
			var product = _db.Products.Find(discountVM.ProductId);
			if (ModelState.IsValid)
			{
				if (discountVM.DiscountType == "fixed")
				{
					if (discountVM.Discount < product.Price)
					{
						dv = new DiscountModel()
						{
							ProductId = discountVM.ProductId,
							DiscountType = discountVM.DiscountType,
							Discount = discountVM.Discount,
							ValidFrom = discountVM.ValidFrom,
							ValidTo = discountVM.ValidTo,
						};
						product.DiscountAmt = product.Price - dv.Discount;
					}
					else
					{
						TempData["error"] = "Discount Amount Should be less than Actual Price!";
						return RedirectToAction("Discount", "Product");
					}
				}
				else if (discountVM.DiscountType == "percent")
				{
					if (discountVM.Discount > 0 && discountVM.Discount <= 100)
					{
						dv = new DiscountModel()
						{
							ProductId = discountVM.ProductId,
							DiscountType = discountVM.DiscountType,
							Discount = discountVM.Discount,
							ValidFrom = discountVM.ValidFrom,
							ValidTo = discountVM.ValidTo,
						};
						double discount = discountVM.Price * dv.Discount / 100;
						product.DiscountAmt = product.Price - discount;
					}
					else
					{
						TempData["error"] = "Discount Percentage Should be in between 0 to 100!";
						return RedirectToAction("Discount", "Product");
					}
				}

				_db.Products.Update(product);
				_db.Discounts.Add(dv);
				_db.SaveChanges();
			}
			return RedirectToAction("Index", "Product");
		}

		// Delete Products
		#region API CALLS
		[HttpDelete]
		[Authorize(Roles = "Dealer, Librarian, Student")]
		public IActionResult Delete(int? id)
		{
			var obj = _db.Products.FirstOrDefault(u => u.Id == id);
			var obj1 = _db.Discounts.ToList();
			if (obj == null)
			{
				return NotFound();
			}
			if (obj.IsOrdered == true)
			{
				TempData["error"] = "Can not Delete this product. User make an order for this product!";
				return Json(new { success = false });
			}

			foreach (var item in obj1)
			{
				if (item.ProductId == id)
				{
					_db.Discounts.Remove(item);
				}
			}

			//delete image
			var oldImagePath = Path.Combine(_hostEnvironment.WebRootPath, obj.ImageUrl.TrimStart('\\'));
			if (System.IO.File.Exists(oldImagePath))
			{
				System.IO.File.Delete(oldImagePath);
			}

			_db.Products.Remove(obj);
			_db.SaveChanges();

			return Json(new { success = true });
		}
		#endregion
	}
}
