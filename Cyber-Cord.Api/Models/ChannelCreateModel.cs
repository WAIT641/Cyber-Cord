using System.ComponentModel.DataAnnotations;

namespace Cyber_Cord.Api.Models;

public class ChannelCreateModel
{
    [Required]
    public string Name { get; set; } = default!;
    [Required]
    public string? Description { get; set; }
}