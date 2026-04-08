using System.ComponentModel.DataAnnotations;

namespace Cyber_Cord.Api.Models;

public class UserServerCreateModel
{
    [Required]
    public int? MemberId { get; set; }
}
