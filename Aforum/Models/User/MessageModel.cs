using Microsoft.AspNetCore.Mvc;

namespace Aforum.Models.User
{
    public class MessageModel
    {
        [FromForm(Name = "comment")]
        public string Message { get; set; } = null!;

        public DateTime CreatedAtMessage { get; set; }

        public String UserName { get; set; } = null!;

        public Guid Id { get; set; }
    }
}
