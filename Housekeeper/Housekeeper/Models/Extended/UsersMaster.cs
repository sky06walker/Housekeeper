using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Housekeeper.Models
{
    [MetadataType(typeof(UsersMasterMetadata))]
    public partial class UsersMaster
    {
        public string ConfirmPassword { get; set; }
    }

    public class UsersMasterMetadata
    {
        [DisplayName("Username")]
        [Required(AllowEmptyStrings =false, ErrorMessage ="Username is required")]
        public string UserName { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [MinLength(8,ErrorMessage ="Minimum 8 characters required")]
        public string Password { get; set; }

        [DisplayName("Confirm Password")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Confirm Password is required")]
        [DataType(DataType.Password)]
        [Compare("Password")]
        public string ConfirmPassword { get; set; }

        [DisplayName("Registration Date")]
        [DataType(DataType.DateTime)]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString ="{0:dd/MM/yyyy}")]
        public Nullable<System.DateTime> RegistrationDate { get; set; }

        [DisplayName("Last Modified Date")]
        [DataType(DataType.DateTime)]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        public Nullable<System.DateTime> LastModified { get; set; }

        [DisplayName("Administrator")]
        public bool isAdmin { get; set; }

        [DisplayName("Email Address")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Email Address is required")]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }
    }
}