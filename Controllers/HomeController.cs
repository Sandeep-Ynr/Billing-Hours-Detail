using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BillingSoftware.Data;
using BillingSoftware.Models.ViewModels;
using System.Diagnostics;
using BillingSoftware.Models;

namespace BillingSoftware.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var clients = await _context.Clients.Include(c => c.Tasks).ToListAsync();
            var tasks = await _context.Tasks.Include(t => t.Client).ToListAsync();

            var viewModel = new DashboardViewModel
            {
                TotalClients = clients.Count,
                ActiveClients = clients.Count(c => c.IsActive),
                TotalTasks = tasks.Count,
                TotalHours = tasks.Sum(t => t.HoursWorked),
                TotalRevenue = tasks.Sum(t => t.HoursWorked * (t.Client?.HourlyRate ?? 0)),
                AverageHourlyRate = clients.Any() ? clients.Average(c => c.HourlyRate) : 0,
                TopClients = tasks
                    .GroupBy(t => new { t.ClientId, t.Client!.Name, t.Client.HourlyRate })
                    .Select(g => new ClientReportViewModel
                    {
                        ClientId = g.Key.ClientId,
                        ClientName = g.Key.Name,
                        HourlyRate = g.Key.HourlyRate,
                        TotalHours = g.Sum(t => t.HoursWorked),
                        TotalIncome = g.Sum(t => t.HoursWorked * g.Key.HourlyRate),
                        TaskCount = g.Count()
                    })
                    .OrderByDescending(c => c.TotalIncome)
                    .Take(5)
                    .ToList(),
                RecentTasks = tasks
                    .OrderByDescending(t => t.TaskDate)
                    .ThenByDescending(t => t.CreatedAt)
                    .Take(10)
                    .Select(t => new RecentTaskViewModel
                    {
                        TaskId = t.Id,
                        ClientName = t.Client?.Name ?? "Unknown",
                        Description = t.Description,
                        TaskDate = t.TaskDate,
                        HoursWorked = t.HoursWorked,
                        Amount = t.HoursWorked * (t.Client?.HourlyRate ?? 0)
                    })
                    .ToList()
            };

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
