using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Housekeeper.Models
{
    public class UserLogin
    {
        [Required(AllowEmptyStrings =false, ErrorMessage ="Username is required")]
        public string Username { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DisplayName("Save login")]
        public bool SaveLogin { get; set; }
    }
}