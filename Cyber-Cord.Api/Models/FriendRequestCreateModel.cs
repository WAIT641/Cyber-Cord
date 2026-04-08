using System.ComponentModel.DataAnnotations;

namespace Cyber_Cord.Api.Models;

public class FriendRequestCreateModel
{
    [Required]
    public int? UserId { get; set; }
}
