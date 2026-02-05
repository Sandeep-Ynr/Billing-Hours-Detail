namespace BillingSoftware.Models.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalClients { get; set; }
        public int ActiveClients { get; set; }
        public int TotalTasks { get; set; }
        public decimal TotalHours { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageHourlyRate { get; set; }
        public List<ClientReportViewModel> TopClients { get; set; } = new();
        public List<RecentTaskViewModel> RecentTasks { get; set; } = new();
    }

    public class ClientReportViewModel
    {
        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public decimal HourlyRate { get; set; }
        public decimal TotalHours { get; set; }
        public decimal TotalIncome { get; set; }
        public int TaskCount { get; set; }
    }

    public class RecentTaskViewModel
    {
        public int TaskId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime TaskDate { get; set; }
        public decimal HoursWorked { get; set; }
        public decimal Amount { get; set; }
    }

    public class ReportsViewModel
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? ClientId { get; set; }
        public List<ClientReportViewModel> ClientReports { get; set; } = new();
        public decimal GrandTotalHours { get; set; }
        public decimal GrandTotalIncome { get; set; }
        public List<Client> Clients { get; set; } = new();
    }

    public class TaskCreateViewModel
    {
        public WorkTask Task { get; set; } = new();
        public List<Client> Clients { get; set; } = new();
    }

    public class TaskEditViewModel
    {
        public WorkTask Task { get; set; } = new();
        public List<Client> Clients { get; set; } = new();
    }
}
