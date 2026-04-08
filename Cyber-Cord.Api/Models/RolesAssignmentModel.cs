using System.ComponentModel.DataAnnotations;

namespace Cyber_Cord.Api.Models;

public class RolesAssignmentModel
{
    [Required]
    public List<string> Roles { get; set; } = default!;
}
