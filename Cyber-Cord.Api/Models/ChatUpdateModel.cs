using System.ComponentModel.DataAnnotations;

namespace Cyber_Cord.Api.Models;

public class ChatUpdateModel
{
    [Required]
    public string Name { get; set; } = default!;
}
