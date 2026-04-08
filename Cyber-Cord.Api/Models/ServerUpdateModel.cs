using System.ComponentModel.DataAnnotations;

namespace Cyber_Cord.Api.Models;

public class ServerUpdateModel
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = default!;
    
    [Required]
    [StringLength(500)]
    public string Description { get; set; } = default!;
}