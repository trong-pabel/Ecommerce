using ECommerceMVC.Data;
using ECommerceMVC.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceMVC.Controllers
{
    public class HangHoaController : Controller
    {
		private readonly EcommerceContext db;

		public HangHoaController(EcommerceContext context)
		{
			db = context;
		}
		public IActionResult Index(int? loai)
        {
			var hangHoas = db.HangHoas.AsQueryable();

			if (loai.HasValue)
			{
				hangHoas = hangHoas.Where(p => p.MaLoai == loai.Value);
			}

			var result = hangHoas.Select(p => new GoodsVM
			{
				MaHh = p.MaHh,
				TenHH = p.TenHh,
				DonGia = p.DonGia ?? 0,
				Hinh = p.Hinh ?? "",
				MoTaNgan = p.MoTaDonVi ?? "",
				TenLoai = p.MaLoaiNavigation.TenLoai
			});
			return View(result);
		}
    }
}
