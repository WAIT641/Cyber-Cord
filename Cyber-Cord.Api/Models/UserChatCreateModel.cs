using System.ComponentModel.DataAnnotations;

namespace Cyber_Cord.Api.Models;

public class UserChatCreateModel
{
    [Required]
    public int? UserId { get; set; }
}
