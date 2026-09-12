using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ObjetosIngresos.Services;

namespace ObjetosIngresos.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class AdminController : Controller
    {
        private readonly AdminServices _adminServices;

        public AdminController(AdminServices adminServices)
        {
            _adminServices = adminServices;
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Index()
        {
            var viewModel = await _adminServices.GetDashboardDataAsync(User);
            return View("~/Views/Admin/Index.cshtml", viewModel);
        }
    }
}
