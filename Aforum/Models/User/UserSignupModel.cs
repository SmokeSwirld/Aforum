using Microsoft.AspNetCore.Mvc;
using System.Xml.Linq;
using System.Text.Json.Serialization;
namespace Aforum.Models.User
{
    public class UserSignupModel
    {
        [FromForm(Name = "user-login")]   // <input name="user-login" ...
        public String Login { get; set; } = null!;

        [FromForm(Name = "user-password")]
        public String Password { get; set; } = null!;

        [FromForm(Name = "user-repeat")]
        public String RepeatPassword { get; set; } = null!;

        [FromForm(Name = "user-name")]
        public String? RealName { get; set; } = null!;

        [FromForm(Name = "user-email")]
        public String Email { get; set; } = null!;
        
        [FromForm(Name = "user-avatar")]
        
        public String AvatarFileName { get; set; } = null!;


        [FromForm(Name = "user-confirm")]
        public Boolean Agree { get; set; }


        public Guid id { get; set; }
        public String type { get; set; } = "Feedback";
        public String? partitionKey { get; set; }
        public DateTime moment { get; set; }

        public List<UserTopicModel> Topics { get; set; } = new List<UserTopicModel>();

        public List<MessageModel> Messages { get; set; } = new List<MessageModel>();
    }
}

