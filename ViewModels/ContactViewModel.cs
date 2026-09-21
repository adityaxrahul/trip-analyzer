using System.ComponentModel.DataAnnotations;

namespace TripAnalyzer.ViewModels
{
    public class ContactViewModel
    {
        [Required(ErrorMessage = "Your Name is required.")]
        [StringLength(100)]
        [Display(Name = "Your Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email Address is required.")]
        [EmailAddress(ErrorMessage = "Invalid Email Address.")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Subject is required.")]
        [StringLength(150)]
        [Display(Name = "Subject")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Message is required.")]
        [StringLength(2000, MinimumLength = 10, ErrorMessage = "Message must be at least 10 characters long.")]
        [Display(Name = "Message")]
        public string Message { get; set; } = string.Empty;

        public bool IsSubmitted { get; set; } = false;
    }
}
