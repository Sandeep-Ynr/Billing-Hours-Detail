using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using BillingSoftware.Data;
using BillingSoftware.Models;
using Microsoft.EntityFrameworkCore;

namespace BillingSoftware.Services
{
    public interface IExportService
    {
        Task<byte[]> ExportClientsToExcelAsync(int? clientId = null, DateTime? startDate = null, DateTime? endDate = null);
        Task<byte[]> ExportClientsToPdfAsync(int? clientId = null, DateTime? startDate = null, DateTime? endDate = null);
        Task<byte[]> ExportClientDetailToExcelAsync(int clientId, DateTime? startDate = null, DateTime? endDate = null);
        Task<byte[]> ExportClientDetailToPdfAsync(int clientId, DateTime? startDate = null, DateTime? endDate = null);
    }

    public class ExportService : IExportService
    {
        private readonly ApplicationDbContext _context;

        public ExportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<byte[]> ExportClientsToExcelAsync(int? clientId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Tasks.Include(t => t.Client).AsQueryable();

            if (clientId.HasValue && clientId.Value > 0)
                query = query.Where(t => t.ClientId == clientId.Value);
            if (startDate.HasValue)
                query = query.Where(t => t.TaskDate >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(t => t.TaskDate <= endDate.Value);

            var tasks = await query.ToListAsync();

            var clientReports = tasks
                .GroupBy(t => new { t.ClientId, t.Client!.Name, t.Client.HourlyRate, t.Client.Email, t.Client.Phone })
                .Select(g => new
                {
                    g.Key.ClientId,
                    ClientName = g.Key.Name,
                    g.Key.HourlyRate,
                    g.Key.Email,
                    g.Key.Phone,
                    TotalHours = g.Sum(t => t.HoursWorked),
                    TotalIncome = g.Sum(t => t.HoursWorked * g.Key.HourlyRate),
                    TaskCount = g.Count()
                })
                .OrderByDescending(r => r.TotalIncome)
                .ToList();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Client Report");

            // Add title
            var titleRange = worksheet.Range("A1:G1");
            titleRange.Merge();
            worksheet.Cell("A1").Value = "Client Report";
            worksheet.Cell("A1").Style.Font.Bold = true;
            worksheet.Cell("A1").Style.Font.FontSize = 16;
            worksheet.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Add date range info
            var dateInfo = $"Period: {(startDate?.ToString("MMM dd, yyyy") ?? "All time")} - {(endDate?.ToString("MMM dd, yyyy") ?? "Present")}";
            worksheet.Cell("A2").Value = dateInfo;
            worksheet.Range("A2:G2").Merge();
            worksheet.Cell("A2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Add headers
            var headers = new[] { "Client Name", "Email", "Phone", "Hourly Rate", "Total Hours", "Task Count", "Total Income" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(4, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#667eea");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // Add data
            int row = 5;
            foreach (var client in clientReports)
            {
                worksheet.Cell(row, 1).Value = client.ClientName;
                worksheet.Cell(row, 2).Value = client.Email ?? "";
                worksheet.Cell(row, 3).Value = client.Phone ?? "";
                worksheet.Cell(row, 4).Value = client.HourlyRate;
                worksheet.Cell(row, 4).Style.NumberFormat.Format = "$#,##0.00";
                worksheet.Cell(row, 5).Value = client.TotalHours;
                worksheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
                worksheet.Cell(row, 6).Value = client.TaskCount;
                worksheet.Cell(row, 7).Value = client.TotalIncome;
                worksheet.Cell(row, 7).Style.NumberFormat.Format = "$#,##0.00";

                // Add alternating row colors
                if (row % 2 == 0)
                {
                    worksheet.Range(row, 1, row, 7).Style.Fill.BackgroundColor = XLColor.FromHtml("#f8f9fa");
                }

                for (int col = 1; col <= 7; col++)
                {
                    worksheet.Cell(row, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }

                row++;
            }

            // Add totals row
            var totalRow = row;
            worksheet.Cell(totalRow, 1).Value = "TOTAL";
            worksheet.Cell(totalRow, 1).Style.Font.Bold = true;
            worksheet.Range(totalRow, 1, totalRow, 4).Merge();
            worksheet.Cell(totalRow, 5).Value = clientReports.Sum(c => c.TotalHours);
            worksheet.Cell(totalRow, 5).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Cell(totalRow, 5).Style.Font.Bold = true;
            worksheet.Cell(totalRow, 6).Value = clientReports.Sum(c => c.TaskCount);
            worksheet.Cell(totalRow, 6).Style.Font.Bold = true;
            worksheet.Cell(totalRow, 7).Value = clientReports.Sum(c => c.TotalIncome);
            worksheet.Cell(totalRow, 7).Style.NumberFormat.Format = "$#,##0.00";
            worksheet.Cell(totalRow, 7).Style.Font.Bold = true;

            for (int col = 1; col <= 7; col++)
            {
                worksheet.Cell(totalRow, col).Style.Fill.BackgroundColor = XLColor.FromHtml("#764ba2");
                worksheet.Cell(totalRow, col).Style.Font.FontColor = XLColor.White;
                worksheet.Cell(totalRow, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<byte[]> ExportClientsToPdfAsync(int? clientId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Tasks.Include(t => t.Client).AsQueryable();

            if (clientId.HasValue && clientId.Value > 0)
                query = query.Where(t => t.ClientId == clientId.Value);
            if (startDate.HasValue)
                query = query.Where(t => t.TaskDate >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(t => t.TaskDate <= endDate.Value);

            var tasks = await query.ToListAsync();

            var clientReports = tasks
                .GroupBy(t => new { t.ClientId, t.Client!.Name, t.Client.HourlyRate, t.Client.Email, t.Client.Phone })
                .Select(g => new
                {
                    g.Key.ClientId,
                    ClientName = g.Key.Name,
                    g.Key.HourlyRate,
                    g.Key.Email,
                    g.Key.Phone,
                    TotalHours = g.Sum(t => t.HoursWorked),
                    TotalIncome = g.Sum(t => t.HoursWorked * g.Key.HourlyRate),
                    TaskCount = g.Count()
                })
                .OrderByDescending(r => r.TotalIncome)
                .ToList();

            var dateRangeText = $"{(startDate?.ToString("MMM dd, yyyy") ?? "All time")} - {(endDate?.ToString("MMM dd, yyyy") ?? "Present")}";

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(column =>
                    {
                        column.Item().Text("Client Report")
                            .FontSize(24)
                            .Bold()
                            .FontColor(Colors.Indigo.Darken3);

                        column.Item().PaddingTop(5).Text(dateRangeText)
                            .FontSize(12)
                            .FontColor(Colors.Grey.Darken1);

                        column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    });

                    page.Content().PaddingVertical(20).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);  // Client Name
                            columns.RelativeColumn(3);  // Email
                            columns.RelativeColumn(2);  // Phone
                            columns.RelativeColumn(1.5f);  // Hourly Rate
                            columns.RelativeColumn(1.5f);  // Total Hours
                            columns.RelativeColumn(1);     // Tasks
                            columns.RelativeColumn(2);     // Total Income
                        });

                        // Header
                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Indigo.Medium).Padding(8)
                                .Text("Client Name").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Indigo.Medium).Padding(8)
                                .Text("Email").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Indigo.Medium).Padding(8)
                                .Text("Phone").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Indigo.Medium).Padding(8)
                                .Text("Rate").FontColor(Colors.White).Bold().AlignRight();
                            header.Cell().Background(Colors.Indigo.Medium).Padding(8)
                                .Text("Hours").FontColor(Colors.White).Bold().AlignRight();
                            header.Cell().Background(Colors.Indigo.Medium).Padding(8)
                                .Text("Tasks").FontColor(Colors.White).Bold().AlignRight();
                            header.Cell().Background(Colors.Indigo.Medium).Padding(8)
                                .Text("Total Income").FontColor(Colors.White).Bold().AlignRight();
                        });

                        // Data rows
                        var isAlternate = false;
                        foreach (var client in clientReports)
                        {
                            var bgColor = isAlternate ? Colors.Grey.Lighten4 : Colors.White;

                            table.Cell().Background(bgColor).Padding(6).Text(client.ClientName);
                            table.Cell().Background(bgColor).Padding(6).Text(client.Email ?? "-");
                            table.Cell().Background(bgColor).Padding(6).Text(client.Phone ?? "-");
                            table.Cell().Background(bgColor).Padding(6).AlignRight().Text($"${client.HourlyRate:N2}");
                            table.Cell().Background(bgColor).Padding(6).AlignRight().Text($"{client.TotalHours:N2}");
                            table.Cell().Background(bgColor).Padding(6).AlignRight().Text($"{client.TaskCount}");
                            table.Cell().Background(bgColor).Padding(6).AlignRight().Text($"${client.TotalIncome:N2}").Bold();

                            isAlternate = !isAlternate;
                        }

                        // Totals row
                        table.Cell().ColumnSpan(4).Background(Colors.Indigo.Darken2).Padding(8)
                            .Text("TOTAL").FontColor(Colors.White).Bold();
                        table.Cell().Background(Colors.Indigo.Darken2).Padding(8).AlignRight()
                            .Text($"{clientReports.Sum(c => c.TotalHours):N2}").FontColor(Colors.White).Bold();
                        table.Cell().Background(Colors.Indigo.Darken2).Padding(8).AlignRight()
                            .Text($"{clientReports.Sum(c => c.TaskCount)}").FontColor(Colors.White).Bold();
                        table.Cell().Background(Colors.Indigo.Darken2).Padding(8).AlignRight()
                            .Text($"${clientReports.Sum(c => c.TotalIncome):N2}").FontColor(Colors.White).Bold();
                    });

                    page.Footer().AlignCenter().Text(txt =>
                    {
                        txt.Span("Generated on ");
                        txt.Span(DateTime.Now.ToString("MMMM dd, yyyy HH:mm")).Bold();
                        txt.Span(" | Page ");
                        txt.CurrentPageNumber();
                        txt.Span(" of ");
                        txt.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }

        public async Task<byte[]> ExportClientDetailToExcelAsync(int clientId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var client = await _context.Clients.FindAsync(clientId);
            if (client == null)
                throw new ArgumentException("Client not found");

            var query = _context.Tasks.Where(t => t.ClientId == clientId);

            if (startDate.HasValue)
                query = query.Where(t => t.TaskDate >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(t => t.TaskDate <= endDate.Value);

            var tasks = await query.OrderByDescending(t => t.TaskDate).ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add($"{client.Name} - Tasks");

            // Client Info Section
            worksheet.Cell("A1").Value = client.Name;
            worksheet.Cell("A1").Style.Font.Bold = true;
            worksheet.Cell("A1").Style.Font.FontSize = 18;
            worksheet.Range("A1:E1").Merge();

            worksheet.Cell("A2").Value = $"Email: {client.Email ?? "N/A"} | Phone: {client.Phone ?? "N/A"} | Rate: ${client.HourlyRate:N2}/hr";
            worksheet.Range("A2:E2").Merge();

            var dateInfo = $"Period: {(startDate?.ToString("MMM dd, yyyy") ?? "All time")} - {(endDate?.ToString("MMM dd, yyyy") ?? "Present")}";
            worksheet.Cell("A3").Value = dateInfo;
            worksheet.Range("A3:E3").Merge();

            // Add headers
            var headers = new[] { "Task Date", "Description", "Task Link", "Hours Worked", "Amount" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(5, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#667eea");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // Add data
            int row = 6;
            foreach (var task in tasks)
            {
                worksheet.Cell(row, 1).Value = task.TaskDate.ToString("MMM dd, yyyy");
                worksheet.Cell(row, 2).Value = task.Description;
                worksheet.Cell(row, 3).Value = task.TaskLink ?? "";
                worksheet.Cell(row, 4).Value = task.HoursWorked;
                worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
                worksheet.Cell(row, 5).Value = task.HoursWorked * client.HourlyRate;
                worksheet.Cell(row, 5).Style.NumberFormat.Format = "$#,##0.00";

                if (row % 2 == 0)
                {
                    worksheet.Range(row, 1, row, 5).Style.Fill.BackgroundColor = XLColor.FromHtml("#f8f9fa");
                }

                for (int col = 1; col <= 5; col++)
                {
                    worksheet.Cell(row, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }

                row++;
            }

            // Totals
            var totalRow = row;
            worksheet.Cell(totalRow, 1).Value = "TOTAL";
            worksheet.Cell(totalRow, 1).Style.Font.Bold = true;
            worksheet.Range(totalRow, 1, totalRow, 3).Merge();
            worksheet.Cell(totalRow, 4).Value = tasks.Sum(t => t.HoursWorked);
            worksheet.Cell(totalRow, 4).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Cell(totalRow, 4).Style.Font.Bold = true;
            worksheet.Cell(totalRow, 5).Value = tasks.Sum(t => t.HoursWorked * client.HourlyRate);
            worksheet.Cell(totalRow, 5).Style.NumberFormat.Format = "$#,##0.00";
            worksheet.Cell(totalRow, 5).Style.Font.Bold = true;

            for (int col = 1; col <= 5; col++)
            {
                worksheet.Cell(totalRow, col).Style.Fill.BackgroundColor = XLColor.FromHtml("#764ba2");
                worksheet.Cell(totalRow, col).Style.Font.FontColor = XLColor.White;
                worksheet.Cell(totalRow, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<byte[]> ExportClientDetailToPdfAsync(int clientId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var client = await _context.Clients.FindAsync(clientId);
            if (client == null)
                throw new ArgumentException("Client not found");

            var query = _context.Tasks.Where(t => t.ClientId == clientId);

            if (startDate.HasValue)
                query = query.Where(t => t.TaskDate >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(t => t.TaskDate <= endDate.Value);

            var tasks = await query.OrderByDescending(t => t.TaskDate).ToListAsync();
            var dateRangeText = $"{(startDate?.ToString("MMM dd, yyyy") ?? "All time")} - {(endDate?.ToString("MMM dd, yyyy") ?? "Present")}";

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(column =>
                    {
                        column.Item().Text(client.Name)
                            .FontSize(24)
                            .Bold()
                            .FontColor(Colors.Indigo.Darken3);

                        column.Item().PaddingTop(5).Row(row =>
                        {
                            if (!string.IsNullOrEmpty(client.Email))
                                row.AutoItem().Text($"📧 {client.Email}  ").FontSize(10);
                            if (!string.IsNullOrEmpty(client.Phone))
                                row.AutoItem().Text($"📞 {client.Phone}  ").FontSize(10);
                            row.AutoItem().Text($"💰 ${client.HourlyRate:N2}/hr").FontSize(10).Bold();
                        });

                        column.Item().PaddingTop(3).Text(dateRangeText)
                            .FontSize(11)
                            .FontColor(Colors.Grey.Darken1);

                        column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    });

                    page.Content().PaddingVertical(20).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);   // Date
                            columns.RelativeColumn(5);   // Description
                            columns.RelativeColumn(1.5f); // Hours
                            columns.RelativeColumn(2);   // Amount
                        });

                        // Header
                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Indigo.Medium).Padding(8)
                                .Text("Date").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Indigo.Medium).Padding(8)
                                .Text("Description").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Indigo.Medium).Padding(8)
                                .Text("Hours").FontColor(Colors.White).Bold().AlignRight();
                            header.Cell().Background(Colors.Indigo.Medium).Padding(8)
                                .Text("Amount").FontColor(Colors.White).Bold().AlignRight();
                        });

                        // Data rows
                        var isAlternate = false;
                        foreach (var task in tasks)
                        {
                            var bgColor = isAlternate ? Colors.Grey.Lighten4 : Colors.White;
                            var amount = task.HoursWorked * client.HourlyRate;

                            table.Cell().Background(bgColor).Padding(6).Text(task.TaskDate.ToString("MMM dd, yyyy"));
                            table.Cell().Background(bgColor).Padding(6).Text(task.Description);
                            table.Cell().Background(bgColor).Padding(6).AlignRight().Text($"{task.HoursWorked:N2}");
                            table.Cell().Background(bgColor).Padding(6).AlignRight().Text($"${amount:N2}");

                            isAlternate = !isAlternate;
                        }

                        // Totals
                        var totalHours = tasks.Sum(t => t.HoursWorked);
                        var totalAmount = tasks.Sum(t => t.HoursWorked * client.HourlyRate);

                        table.Cell().ColumnSpan(2).Background(Colors.Indigo.Darken2).Padding(8)
                            .Text("TOTAL").FontColor(Colors.White).Bold();
                        table.Cell().Background(Colors.Indigo.Darken2).Padding(8).AlignRight()
                            .Text($"{totalHours:N2}").FontColor(Colors.White).Bold();
                        table.Cell().Background(Colors.Indigo.Darken2).Padding(8).AlignRight()
                            .Text($"${totalAmount:N2}").FontColor(Colors.White).Bold();
                    });

                    page.Footer().AlignCenter().Text(txt =>
                    {
                        txt.Span("Generated on ");
                        txt.Span(DateTime.Now.ToString("MMMM dd, yyyy HH:mm")).Bold();
                        txt.Span(" | Page ");
                        txt.CurrentPageNumber();
                        txt.Span(" of ");
                        txt.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}
