using System.ComponentModel.DataAnnotations;

namespace Cyber_Cord.Api.Models;

public class ColorCreateModel
{
    [Required, Range(0, 255)]
    public int? Red { get; set; }
    [Required, Range(0, 255)]
    public int? Green { get; set; }
    [Required, Range(0, 255)]
    public int? Blue { get; set; }
}
