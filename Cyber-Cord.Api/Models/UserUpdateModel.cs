using System.ComponentModel.DataAnnotations;
using Shared;

namespace Cyber_Cord.Api.Models;

public class UserUpdateModel
{
    [Required, MinLength(GlobalConstants.MinNameLength)]
    public string DisplayName { get; set; } = default!;
    public string Description { get; set; } = string.Empty;
    [Required]
    public ColorCreateModel BannerColor { get; set; } = default!;
}
