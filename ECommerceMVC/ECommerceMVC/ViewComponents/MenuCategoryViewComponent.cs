using ECommerceMVC.Data;
using ECommerceMVC.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceMVC.ViewComponents
{
    public class MenuCategoryViewComponent : ViewComponent
    {
        private readonly EcommerceContext db;

        public MenuCategoryViewComponent(EcommerceContext context) => db = context;
        public IViewComponentResult Invoke()
        {
            var data = db.Loais.Select(lo => new MenuCategoryVM
            {
                MaLoai = lo.MaLoai,
                TenLoai = lo.TenLoai,
                SoLuong = lo.HangHoas.Count
            }).OrderBy(p => p.TenLoai); ;
            return View("MenuCategoryPage", data); //Default.cshtml
            // return View("Default" ,data);
        }
    }
}
