using System.ComponentModel.DataAnnotations;
using System.Net.NetworkInformation;


namespace ASC.Web.Areas.Accounts.Models
{
    public class CustomerRegistrationViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        public string UserName { get; set; }

        public bool IsEdit { get; set; }

        public bool IsActive { get; set; }
    }
}
