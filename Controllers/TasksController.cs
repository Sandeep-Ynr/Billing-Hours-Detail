using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BillingSoftware.Data;
using BillingSoftware.Models;
using BillingSoftware.Models.ViewModels;

namespace BillingSoftware.Controllers
{
    public class TasksController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TasksController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Tasks
        public async Task<IActionResult> Index(int? clientId, DateTime? startDate, DateTime? endDate, int? year, int? month)
        {
            var query = _context.Tasks
                .Include(t => t.Client)
                .AsQueryable();

            // 1. Client Filter
            if (clientId.HasValue)
            {
                query = query.Where(t => t.ClientId == clientId.Value);
            }

            // 2. Date Filter Logic
            if (startDate.HasValue || endDate.HasValue)
            {
                // Date Range Priority
                if (startDate.HasValue)
                    query = query.Where(t => t.TaskDate >= startDate.Value);
                
                if (endDate.HasValue)
                    query = query.Where(t => t.TaskDate <= endDate.Value);
                
                // Clear Year/Month if Date Range is active
                year = null; 
                month = null;
            }
            else if (year.HasValue && month.HasValue)
            {
                // Year/Month Filter
                query = query.Where(t => t.TaskDate.Year == year.Value && t.TaskDate.Month == month.Value);
            }
            else
            {
                // Default: Current Month if no filters at all? 
                // Creating a default to show *something* relevant if the user visits /Tasks directly
                // But for now, let's keep it showing all or maybe default to current month to be consistent with Reports?
                // The user said "filter data year and month wise", implying they want that control.
                // Let's default to verified behavior: if no params, show all? Or distinct? 
                // Existing behavior was "Show All". 
                // Let's default to Current Month/Year to avoid loading thousands of tasks.
                if (!year.HasValue && !month.HasValue)
                {
                   year = DateTime.Now.Year;
                   month = DateTime.Now.Month;
                   query = query.Where(t => t.TaskDate.Year == year.Value && t.TaskDate.Month == month.Value);
                }
            }

            var tasks = await query
                .OrderByDescending(t => t.TaskDate)
                .ThenByDescending(t => t.CreatedAt)
                .ToListAsync();

            // Prepare Dropdown Data
            // We need all available years from the DB for the dropdown
            var availableYears = await _context.Tasks
                .Select(t => t.TaskDate.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();
            
            if (!availableYears.Any()) availableYears.Add(DateTime.Now.Year);
            if (!availableYears.Contains(DateTime.Now.Year)) availableYears.Insert(0, DateTime.Now.Year);

            ViewBag.Clients = await _context.Clients.Where(c => c.IsActive).ToListAsync();
            ViewBag.SelectedClientId = clientId;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            ViewBag.SelectedYear = year;
            ViewBag.SelectedMonth = month;
            ViewBag.AvailableYears = availableYears;

            return View(tasks);
        }

        // GET: Tasks/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var task = await _context.Tasks
                .Include(t => t.Client)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (task == null)
            {
                return NotFound();
            }

            return View(task);
        }

        // GET: Tasks/Create
        public async Task<IActionResult> Create(int? clientId)
        {
            var viewModel = new TaskCreateViewModel
            {
                Clients = await _context.Clients.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync(),
                Task = new WorkTask
                {
                    TaskDate = DateTime.Today,
                    ClientId = clientId ?? 0
                }
            };
            return View(viewModel);
        }

        // POST: Tasks/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TaskCreateViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                viewModel.Task.CreatedAt = DateTime.Now;
                _context.Add(viewModel.Task);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Task created successfully!";
                return RedirectToAction(nameof(Index));
            }
            viewModel.Clients = await _context.Clients.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
            return View(viewModel);
        }

        // GET: Tasks/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
            {
                return NotFound();
            }

            var viewModel = new TaskEditViewModel
            {
                Task = task,
                Clients = await _context.Clients.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync()
            };
            return View(viewModel);
        }

        // POST: Tasks/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TaskEditViewModel viewModel)
        {
            if (id != viewModel.Task.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(viewModel.Task);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Task updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TaskExists(viewModel.Task.Id))
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
            viewModel.Clients = await _context.Clients.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
            return View(viewModel);
        }

        // GET: Tasks/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var task = await _context.Tasks
                .Include(t => t.Client)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (task == null)
            {
                return NotFound();
            }

            return View(task);
        }

        // POST: Tasks/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task != null)
            {
                _context.Tasks.Remove(task);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Task deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool TaskExists(int id)
        {
            return _context.Tasks.Any(e => e.Id == id);
        }
    }
}
