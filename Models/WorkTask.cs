using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillingSoftware.Models
{
    public class WorkTask
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Client is required")]
        [Display(Name = "Client")]
        public int ClientId { get; set; }

        [Required(ErrorMessage = "Task date is required")]
        [Display(Name = "Task Date")]
        [DataType(DataType.Date)]
        public DateTime TaskDate { get; set; } = DateTime.Today;

        [StringLength(500, ErrorMessage = "Task link cannot exceed 500 characters")]
        [Display(Name = "Task Link")]
        [Url(ErrorMessage = "Invalid URL format")]
        public string? TaskLink { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        [Display(Name = "Task Description")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Hours worked is required")]
        [Range(0.25, 24, ErrorMessage = "Hours must be between 0.25 and 24")]
        [Display(Name = "Hours Worked")]
        public decimal HoursWorked { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation property
        [ForeignKey("ClientId")]
        public Client? Client { get; set; }

        // Calculated property
        [NotMapped]
        public decimal TotalAmount => Client != null ? HoursWorked * Client.HourlyRate : 0;
    }
}
