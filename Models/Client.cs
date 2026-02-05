using System.ComponentModel.DataAnnotations;

namespace BillingSoftware.Models
{
    public class Client
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Client name is required")]
        [StringLength(100, ErrorMessage = "Client name cannot exceed 100 characters")]
        [Display(Name = "Client Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Hourly rate is required")]
        [Range(0.01, 10000, ErrorMessage = "Hourly rate must be between 0.01 and 10,000")]
        [Display(Name = "Hourly Rate ($)")]
        [DataType(DataType.Currency)]
        public decimal HourlyRate { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "Invalid phone number")]
        public string? Phone { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        // Navigation property
        public ICollection<WorkTask> Tasks { get; set; } = new List<WorkTask>();
    }
}
