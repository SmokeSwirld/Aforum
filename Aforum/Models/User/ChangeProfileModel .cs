using Microsoft.AspNetCore.Mvc;

namespace Aforum.Models.User
{
    public class ChangeProfileModel
    {
        [FromForm(Name = "new-name")]
        public string NewName { get; set; }=null! ;
        [FromForm(Name = "new-password")]
        public string NewPassword { get; set; } = null!;
       
    }
}
