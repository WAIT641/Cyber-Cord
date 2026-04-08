using System.ComponentModel.DataAnnotations;

namespace Cyber_Cord.Api.Models;

public class MessageCreateModel
{
    [Required]
    public string Content { get; set; } = default!;
}