using ECommerceMVC.Data;
using ECommerceMVC.ViewModels;
using Microsoft.AspNetCore.Mvc;
using ECommerceMVC.Helpers;
using Microsoft.AspNetCore.Authorization;
using ECommerceMVC.Services;

namespace ECommerceMVC.Controllers
{
	public class CartController : Controller
	{
		private readonly EcommerceContext db;
		private readonly IVnPayService _vnPayService;

		public CartController(EcommerceContext context, IVnPayService vnPayService)
		{
			db = context;
			_vnPayService = vnPayService;
		}
		public List<CartItem> Cart => HttpContext.Session.Get<List<CartItem>>(MySetting.CART_KEY) ?? new List<CartItem>();
		public IActionResult Index()
		{
			return View(Cart);
		}
		public IActionResult AddToCart(int id, int quantity = 1)
		{
			var gioHang = Cart;
			var item = gioHang.SingleOrDefault(p => p.MaHh == id);
			if (item == null)
			{
				var hangHoa = db.HangHoas.SingleOrDefault(p => p.MaHh == id);
				if (hangHoa == null)
				{
					TempData["Message"] = $"Không tìm thấy hàng hóa có mã {id}";
					return Redirect("/404");
				}
				item = new CartItem
				{
					MaHh = hangHoa.MaHh,
					TenHH = hangHoa.TenHh,
					DonGia = hangHoa.DonGia ?? 0,
					Hinh = hangHoa.Hinh ?? string.Empty,
					SoLuong = quantity
				};
				gioHang.Add(item);
			}
			else
			{
				item.SoLuong += quantity;
			}

			HttpContext.Session.Set(MySetting.CART_KEY, gioHang);

			return RedirectToAction("Index");
		}
		public IActionResult RemoveCart(int id)
		{
			var gioHang = Cart;
			var item = gioHang.SingleOrDefault(p => p.MaHh == id);
			if (item != null)
			{
				gioHang.Remove(item);
				HttpContext.Session.Set(MySetting.CART_KEY, gioHang);
			}
			return RedirectToAction("Index");
		}

		[Authorize]
		[HttpGet]
		public IActionResult Checkout()
		{
			if (Cart.Count == 0)
			{
				return Redirect("/");
			}

			return View(Cart);
		}

		[Authorize]
		[HttpPost]
		public IActionResult Checkout(CheckoutVM model, string payment = "COD")
		{
			if (ModelState.IsValid)
			{
				if (payment == "Place Order by VNPAY")
				{
					var vnPayModel = new VnPaymentRequestModel
					{
						Amount = Cart.Sum(p => p.ThanhTien),
						CreatedDate = DateTime.Now,
						Description = $"{model.HoTen} {model.DienThoai}",
						FullName = model.HoTen,
						OrderId = new Random().Next(1000, 100000)
					};
					#region DBEXCEPT
					// cần sửa lại chỗ này để đẹp hơn
					var customerId1 = HttpContext.User.Claims.SingleOrDefault(p => p.Type == MySetting.CLAIM_CUSTOMERID).Value;
					var khachHang1 = new KhachHang();
					if (model.GiongKhachHang)
					{
						khachHang1 = db.KhachHangs.SingleOrDefault(kh => kh.MaKh == customerId1);
					}

					var hoadon1 = new HoaDon
					{
						MaKh = customerId1,
						HoTen = model.HoTen ?? khachHang1.HoTen,
						DiaChi = model.DiaChi ?? khachHang1.DiaChi,
						DienThoai = model.DienThoai ?? khachHang1.DienThoai,
						NgayDat = DateTime.Now,
						CachThanhToan = "VNPAY",
						CachVanChuyen = "GRAB",
						MaTrangThai = 0,
						GhiChu = model.GhiChu
					};

					db.Database.BeginTransaction();
					try
					{
						db.Database.CommitTransaction();
						db.Add(hoadon1);
						db.SaveChanges();

						var chiTietHoaDon = new List<ChiTietHd>();
						foreach (var item in Cart)
						{
							chiTietHoaDon.Add(new ChiTietHd
							{
								MaHd = hoadon1.MaHd,
								SoLuong = item.SoLuong,
								DonGia = item.DonGia,
								MaHh = item.MaHh,
								GiamGia = 0
							});
						}
						db.AddRange(chiTietHoaDon);
						db.SaveChanges();
						HttpContext.Session.Set<List<CartItem>>(MySetting.CART_KEY, new List<CartItem>());
					}
					catch (Exception ex)
					{
						db.Database.RollbackTransaction();
					}
					#endregion
					return Redirect(_vnPayService.CreatePaymentUrl(HttpContext, vnPayModel));
				}
				var customerId = HttpContext.User.Claims.SingleOrDefault(p => p.Type == MySetting.CLAIM_CUSTOMERID).Value;
				var khachHang = new KhachHang();
				if (model.GiongKhachHang)
				{
					khachHang = db.KhachHangs.SingleOrDefault(kh => kh.MaKh == customerId);
				}

				var hoadon = new HoaDon
				{
					MaKh = customerId,
					HoTen = model.HoTen ?? khachHang.HoTen,
					DiaChi = model.DiaChi ?? khachHang.DiaChi,
					DienThoai = model.DienThoai ?? khachHang.DienThoai,
					NgayDat = DateTime.Now,
					CachThanhToan = "COD",
					CachVanChuyen = "GRAB",
					MaTrangThai = 0,
					GhiChu = model.GhiChu
				};

				db.Database.BeginTransaction();
				try
				{
					db.Database.CommitTransaction();
					db.Add(hoadon);
					db.SaveChanges();

					var chiTietHoaDon = new List<ChiTietHd>();
					foreach (var item in Cart)
					{
						chiTietHoaDon.Add(new ChiTietHd
						{
							MaHd = hoadon.MaHd,
							SoLuong = item.SoLuong,
							DonGia = item.DonGia,
							MaHh = item.MaHh,
							GiamGia = 0
						});
					}
					db.AddRange(chiTietHoaDon);
					db.SaveChanges();
					HttpContext.Session.Set<List<CartItem>>(MySetting.CART_KEY, new List<CartItem>());
					return View("Success");
				}
				catch (Exception ex)
				{
					db.Database.RollbackTransaction();
				}

			}
			return View(Cart);
		}

		[Authorize]
		public IActionResult PaymentSuccess()
		{
			return View("Success");
		}

		[Authorize]
		public IActionResult PaymentFail()
		{
			return View();
		}

		[Authorize]
		public IActionResult PaymentCallBack()
		{
			var response = _vnPayService.PaymentExecute(Request.Query);

			if (response == null || response.VnPayResponseCode != "00")
			{
				TempData["Message"] = $"Lỗi thanh toán VN Pay: {response.VnPayResponseCode}";
				return RedirectToAction("PaymentFail");
			}

			//Lưu đơn hàng vào db
			CheckoutVM model = new CheckoutVM();
			var customerId = HttpContext.User.Claims.SingleOrDefault(p => p.Type == MySetting.CLAIM_CUSTOMERID).Value;
			var khachHang = new KhachHang();
			if (model.GiongKhachHang)
			{
				khachHang = db.KhachHangs.SingleOrDefault(kh => kh.MaKh == customerId);
			}

			var hoadon = new HoaDon
			{
				MaKh = customerId,
				HoTen = model.HoTen ?? khachHang.HoTen,
				DiaChi = model.DiaChi ?? khachHang.DiaChi,
				DienThoai = model.DienThoai ?? khachHang.DienThoai,
				NgayDat = DateTime.Now,
				CachThanhToan = "VNPAY",
				CachVanChuyen = "GRAB",
				MaTrangThai = 0,
				GhiChu = model.GhiChu
			};

			db.Database.BeginTransaction();
			try
			{

				db.Add(hoadon);
				db.SaveChanges();

				var cthds = new List<ChiTietHd>();
				foreach (var item in Cart)
				{
					cthds.Add(new ChiTietHd
					{
						MaHd = hoadon.MaHd,
						SoLuong = item.SoLuong,
						DonGia = item.DonGia,
						MaHh = item.MaHh,
						GiamGia = 0
					});
				}
				db.AddRange(cthds);
				db.SaveChanges();
				db.Database.CommitTransaction();

				HttpContext.Session.Set<List<CartItem>>(MySetting.CART_KEY, new List<CartItem>());

			}
			catch
			{
				db.Database.RollbackTransaction();
			}

			TempData["Message"] = $"Thanh toán VNPay thành công";
			return RedirectToAction("PaymentSuccess");
		}
	}
}
