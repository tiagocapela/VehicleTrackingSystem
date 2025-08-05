// ================================================================================================
// src/VehicleTracking.Web/Controllers/VehicleController.cs
// ================================================================================================
using Microsoft.AspNetCore.Mvc;
using VehicleTracking.Web.Models;
using VehicleTracking.Web.Services;

namespace VehicleTracking.Web.Controllers
{
    public class VehicleController : Controller
    {
        private readonly IVehicleService _vehicleService;

        public VehicleController(IVehicleService vehicleService)
        {
            _vehicleService = vehicleService;
        }

        public async Task<IActionResult> Index()
        {
            var vehicles = await _vehicleService.GetAllVehiclesAsync();
            return View(vehicles);
        }

        public async Task<IActionResult> Details(int id)
        {
            var vehicle = await _vehicleService.GetVehicleByIdAsync(id);
            if (vehicle == null)
                return NotFound();

            return View(vehicle);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Vehicle vehicle)
        {
            if (ModelState.IsValid)
            {
                await _vehicleService.CreateVehicleAsync(vehicle);
                return RedirectToAction(nameof(Index));
            }
            return View(vehicle);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var vehicle = await _vehicleService.GetVehicleByIdAsync(id);
            if (vehicle == null)
                return NotFound();

            return View(vehicle);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Vehicle vehicle)
        {
            if (id != vehicle.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                await _vehicleService.UpdateVehicleAsync(vehicle);
                return RedirectToAction(nameof(Index));
            }
            return View(vehicle);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var vehicle = await _vehicleService.GetVehicleByIdAsync(id);
            if (vehicle == null)
                return NotFound();

            return View(vehicle);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _vehicleService.DeleteVehicleAsync(id);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Track(int id)
        {
            var vehicle = await _vehicleService.GetVehicleByIdAsync(id);
            if (vehicle == null)
                return NotFound();

            return View(vehicle);
        }

        [HttpGet]
        public async Task<IActionResult> History(int id, DateTime? from, DateTime? to)
        {
            var vehicle = await _vehicleService.GetVehicleByIdAsync(id);
            if (vehicle == null)
                return NotFound();

            // Set default date range if not provided
            from ??= DateTime.Today.AddDays(-7);
            to ??= DateTime.Now;

            // Ensure 'to' is not in the future
            if (to > DateTime.Now)
                to = DateTime.Now;

            // Ensure date range is not too large (max 30 days)
            if ((to.Value - from.Value).TotalDays > 30)
                from = to.Value.AddDays(-30);

            var locations = await _vehicleService.GetVehicleLocationsAsync(id, from, to);
            
            ViewBag.From = from;
            ViewBag.To = to;
            ViewBag.Locations = locations;

            return View(vehicle);
        }

        [HttpGet]
        public async Task<IActionResult> ExportHistory(int id, DateTime? from, DateTime? to, string format = "csv")
        {
            var vehicle = await _vehicleService.GetVehicleByIdAsync(id);
            if (vehicle == null)
                return NotFound();

            from ??= DateTime.Today.AddDays(-7);
            to ??= DateTime.Now;

            try
            {
                var fileBytes = await _vehicleService.ExportVehicleHistoryAsync(id, from.Value, to.Value, format);
                var fileName = $"{vehicle.VehicleName.Replace(" ", "_")}_History_{from.Value:yyyyMMdd}_{to.Value:yyyyMMdd}.csv";
                
                return File(fileBytes, "text/csv", fileName);
            }
            catch (NotSupportedException)
            {
                return BadRequest("Unsupported export format");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error exporting data: {ex.Message}");
            }
        }
    }
}

