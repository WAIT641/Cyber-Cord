using System.ComponentModel.DataAnnotations;
using Shared;

namespace Cyber_Cord.Api.Models;

public class UserPasswordChangeModel
{
    [Required, MinLength(GlobalConstants.MinPasswordLength)]
    public string NewPassword { get; set; } = default!;

    [Required]
    public string OldPassword { get; set; } = default!;

}
