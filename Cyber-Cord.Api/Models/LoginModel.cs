using System.ComponentModel.DataAnnotations;

namespace Cyber_Cord.Api.Models;

public class LoginModel
{
    [Required, MinLength(0)]
    public string UserName { get; set; } = default!;
    [Required, MinLength(0)]
    public string Password { get; set; } = default!;
}