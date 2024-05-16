using Microsoft.AspNetCore.Identity;
using RentalBook.Models.ViewModels;
using System.ComponentModel.DataAnnotations;

namespace RentalBook.Models.Authentication
{
    public class ApplicationUser : IdentityUser
    {
        public string? Area { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PinCode { get; set; }
        public StatusType StatusTypes { get; set; }
        public bool IsActive { get; set; } = true;

        public string? Reason { get; set; }
    }
}
