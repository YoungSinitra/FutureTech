using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace FutureTech.Models
{
    public class Student
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }
        
        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }
        
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        
        [Required]
        [Display(Name = "Mobile Number")]
        [Phone]
        public string MobileNumber { get; set; }
        
        [Required]
        [Display(Name = "Enrolment Status")]
        public string EnrolmentStatus { get; set; } = "Active";
        
        [Display(Name = "Profile Image URL")]
        public string? ProfileImageUrl { get; set; }
        
        [Display(Name = "Full Name")]
        public string FullName => $"{FirstName} {LastName}";
    }
} 