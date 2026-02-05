using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BillingSoftware.Data;
using BillingSoftware.Models;
using BillingSoftware.Models.ViewModels;
using BillingSoftware.Services;

namespace BillingSoftware.Controllers
{
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IExportService _exportService;

        public ReportsController(ApplicationDbContext context, IExportService exportService)
        {
            _context = context;
            _exportService = exportService;
        }

        // GET: Reports
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate, int? clientId)
        {
            // Default to current month if no dates specified
            if (!startDate.HasValue && !endDate.HasValue)
            {
                startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                endDate = DateTime.Now;
            }

            var query = _context.Tasks
                .Include(t => t.Client)
                .AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(t => t.TaskDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(t => t.TaskDate <= endDate.Value);
            }

            if (clientId.HasValue && clientId.Value > 0)
            {
                query = query.Where(t => t.ClientId == clientId.Value);
            }

            var tasks = await query.ToListAsync();

            var reportData = tasks
                .GroupBy(t => new { t.ClientId, t.Client!.Name, t.Client.HourlyRate, t.Client.Currency, t.Client.ConversionRate })
                .Select(g => new
                {
                    ClientId = g.Key.ClientId,
                    ClientName = g.Key.Name,
                    HourlyRate = g.Key.HourlyRate,
                    Currency = g.Key.Currency,
                    ConversionRate = g.Key.ConversionRate,
                    TotalHours = g.Sum(t => t.HoursWorked),
                    NativeIncome = g.Sum(t => t.HoursWorked * g.Key.HourlyRate),
                    TaskCount = g.Count()
                })
                .ToList();

            var clientReports = reportData
                .Select(r => new ClientReportViewModel
                {
                    ClientId = r.ClientId,
                    ClientName = r.ClientName,
                    HourlyRate = r.HourlyRate,
                    Currency = r.Currency, // Ensure this property is added to ViewModel
                    TotalHours = r.TotalHours,
                    TotalIncome = r.NativeIncome,
                    TotalIncomeInInr = r.Currency == "USD" ? r.NativeIncome * r.ConversionRate : r.NativeIncome,
                    TaskCount = r.TaskCount
                })
                .OrderByDescending(r => r.TotalIncome)
                .ToList();

            var grandTotalIncome = reportData.Sum(r => r.Currency == "USD" ? r.NativeIncome * r.ConversionRate : r.NativeIncome);

            var viewModel = new ReportsViewModel
            {
                StartDate = startDate,
                EndDate = endDate,
                ClientId = clientId,
                ClientReports = clientReports,
                GrandTotalHours = clientReports.Sum(r => r.TotalHours),
                GrandTotalIncome = grandTotalIncome,
                Clients = await _context.Clients.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync()
            };

            return View(viewModel);
        }

        // GET: Reports/ClientDetails/5
        public async Task<IActionResult> ClientDetails(int id, DateTime? startDate, DateTime? endDate)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null)
            {
                return NotFound();
            }

            var query = _context.Tasks
                .Where(t => t.ClientId == id)
                .AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(t => t.TaskDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(t => t.TaskDate <= endDate.Value);
            }

            var tasks = await query
                .OrderByDescending(t => t.TaskDate)
                .ToListAsync();

            ViewBag.Client = client;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            ViewBag.TotalHours = tasks.Sum(t => t.HoursWorked);
            ViewBag.TotalIncome = tasks.Sum(t => t.HoursWorked * client.HourlyRate);

            return View(tasks);
        }

        // GET: Reports/MonthlyBreakdown
        public async Task<IActionResult> MonthlyBreakdown(int? year)
        {
            year ??= DateTime.Now.Year;

            var tasks = await _context.Tasks
                .Include(t => t.Client)
                .Where(t => t.TaskDate.Year == year)
                .ToListAsync();

            var monthlyData = tasks
                .GroupBy(t => t.TaskDate.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    MonthName = new DateTime(year.Value, g.Key, 1).ToString("MMMM"),
                    TotalHours = g.Sum(t => t.HoursWorked),
                    TotalIncome = g.Sum(t => t.HoursWorked * t.Client!.HourlyRate),
                    TaskCount = g.Count()
                })
                .OrderBy(m => m.Month)
                .ToList();

            ViewBag.Year = year;
            ViewBag.AvailableYears = await _context.Tasks
                .Select(t => t.TaskDate.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();

            return View(monthlyData);
        }

        // GET: Reports/ExportToExcel
        public async Task<IActionResult> ExportToExcel(DateTime? startDate, DateTime? endDate, int? clientId)
        {
            var excelData = await _exportService.ExportClientsToExcelAsync(clientId, startDate, endDate);
            var fileName = $"Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // GET: Reports/ExportToPdf
        public async Task<IActionResult> ExportToPdf(DateTime? startDate, DateTime? endDate, int? clientId)
        {
            var pdfData = await _exportService.ExportClientsToPdfAsync(clientId, startDate, endDate);
            var fileName = $"Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(pdfData, "application/pdf", fileName);
        }

        // GET: Reports/ExportClientDetailsToExcel
        public async Task<IActionResult> ExportClientDetailsToExcel(int id, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var client = await _context.Clients.FindAsync(id);
                if (client == null)
                    return NotFound();

                var excelData = await _exportService.ExportClientDetailToExcelAsync(id, startDate, endDate);
                var fileName = $"{client.Name.Replace(" ", "_")}_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (ArgumentException)
            {
                return NotFound();
            }
        }

        // GET: Reports/ExportClientDetailsToPdf
        public async Task<IActionResult> ExportClientDetailsToPdf(int id, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var client = await _context.Clients.FindAsync(id);
                if (client == null)
                    return NotFound();

                var pdfData = await _exportService.ExportClientDetailToPdfAsync(id, startDate, endDate);
                var fileName = $"{client.Name.Replace(" ", "_")}_Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                return File(pdfData, "application/pdf", fileName);
            }
            catch (ArgumentException)
            {
                return NotFound();
            }
        }
    }
}
