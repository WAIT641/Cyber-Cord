using System.ComponentModel.DataAnnotations;
using Shared;

namespace Cyber_Cord.Api.Models;

public class ServerCreateModel
{
    [Required]
    [StringLength(GlobalConstants.MaxNameLength, MinimumLength = 1)]
    public string? Name { get; set; }
    
    [StringLength(GlobalConstants.MaxDescriptionLength)]
    public string? Description { get; set; }
}