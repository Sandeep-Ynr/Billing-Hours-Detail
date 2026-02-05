using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BillingSoftware.Data;
using BillingSoftware.Models;
using BillingSoftware.Services;

namespace BillingSoftware.Controllers
{
    public class ClientsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IExportService _exportService;

        public ClientsController(ApplicationDbContext context, IExportService exportService)
        {
            _context = context;
            _exportService = exportService;
        }

        // GET: Clients
        public async Task<IActionResult> Index()
        {
            var clients = await _context.Clients
                .Include(c => c.Tasks)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
            return View(clients);
        }

        // GET: Clients/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var client = await _context.Clients
                .Include(c => c.Tasks)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (client == null)
            {
                return NotFound();
            }

            return View(client);
        }

        // GET: Clients/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Clients/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,HourlyRate,Description,Email,Phone,IsActive")] Client client)
        {
            if (ModelState.IsValid)
            {
                client.CreatedAt = DateTime.Now;
                _context.Add(client);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Client '{client.Name}' created successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(client);
        }

        // GET: Clients/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var client = await _context.Clients.FindAsync(id);
            if (client == null)
            {
                return NotFound();
            }
            return View(client);
        }

        // POST: Clients/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,HourlyRate,Description,Email,Phone,IsActive,CreatedAt")] Client client)
        {
            if (id != client.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(client);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Client '{client.Name}' updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClientExists(client.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(client);
        }

        // GET: Clients/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var client = await _context.Clients
                .Include(c => c.Tasks)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (client == null)
            {
                return NotFound();
            }

            return View(client);
        }

        // POST: Clients/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client != null)
            {
                var clientName = client.Name;
                _context.Clients.Remove(client);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Client '{clientName}' deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Clients/ExportAllToExcel
        public async Task<IActionResult> ExportAllToExcel()
        {
            var excelData = await _exportService.ExportClientsToExcelAsync();
            var fileName = $"AllClients_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // GET: Clients/ExportAllToPdf
        public async Task<IActionResult> ExportAllToPdf()
        {
            var pdfData = await _exportService.ExportClientsToPdfAsync();
            var fileName = $"AllClients_Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(pdfData, "application/pdf", fileName);
        }

        // GET: Clients/ExportToExcel/5
        public async Task<IActionResult> ExportToExcel(int id, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var client = await _context.Clients.FindAsync(id);
                if (client == null)
                    return NotFound();

                var excelData = await _exportService.ExportClientDetailToExcelAsync(id, startDate, endDate);
                var fileName = $"{client.Name.Replace(" ", "_")}_Tasks_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (ArgumentException)
            {
                return NotFound();
            }
        }

        // GET: Clients/ExportToPdf/5
        public async Task<IActionResult> ExportToPdf(int id, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var client = await _context.Clients.FindAsync(id);
                if (client == null)
                    return NotFound();

                var pdfData = await _exportService.ExportClientDetailToPdfAsync(id, startDate, endDate);
                var fileName = $"{client.Name.Replace(" ", "_")}_Tasks_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                return File(pdfData, "application/pdf", fileName);
            }
            catch (ArgumentException)
            {
                return NotFound();
            }
        }

        private bool ClientExists(int id)
        {
            return _context.Clients.Any(e => e.Id == id);
        }
    }
}

