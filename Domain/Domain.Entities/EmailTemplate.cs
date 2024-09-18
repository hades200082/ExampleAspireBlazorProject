using System.ComponentModel.DataAnnotations;
using Domain.Abstractions;

namespace Domain.Entities;

public sealed class EmailTemplate : SimpleEntity<string>
{
    [Required]
    [MinLength(20)]
    public string Template { get; set; }
}