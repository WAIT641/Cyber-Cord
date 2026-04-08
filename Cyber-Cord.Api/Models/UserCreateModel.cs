using System.ComponentModel.DataAnnotations;
using Cyber_Cord.Api.Attributes;
using Shared;

namespace Cyber_Cord.Api.Models;

public class UserCreateModel
{
    [Required, MinLength(GlobalConstants.MinNameLength), NoWhiteSpace]
    public string Name { get; set; } = default!;
    [Required, MinLength(GlobalConstants.MinNameLength)]
    public string DisplayName { get; set; } = default!;
    [Required, MinLength(GlobalConstants.MinPasswordLength), NoWhiteSpace]
    public string Password { get; set; } = default!;
    [Required, EmailAddress]
    public string Email { get; set; } = default!;
    public string Description { get; set; } = string.Empty;
    [Required]
    public ColorCreateModel BannerColor { get; set; } = default!;
}
