using System.ComponentModel.DataAnnotations;

namespace Cyber_Cord.Api.Models;

public class ChatCreateModel
{
    [Required]
    public string Name { get; set; } = default!;
}
