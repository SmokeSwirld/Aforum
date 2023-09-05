using Microsoft.AspNetCore.Mvc;

namespace Aforum.Models.User
{
    public class UserTopicModel
    {
        public Guid Id { get; set; }

        [FromForm(Name = "user-topicname")]
        public string Title { get; set; } = null!;

        [FromForm(Name = "user-description")]
        public string Description { get; set; } = null!;

        [FromForm(Name = "user-photo")]
        public String PhotoFileName { get; set; } = null!;
        [FromForm(Name = "user-PhotoPassword")]
        public String PhotoPassword { get; set; } = null!;

        public DateTime CreatedAtTopic { get; set; }

        public String UserName { get; set; } = null!;
        


    }
}
