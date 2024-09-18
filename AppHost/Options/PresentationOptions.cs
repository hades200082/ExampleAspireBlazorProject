using System.ComponentModel.DataAnnotations;

namespace AppHost.Options;

internal sealed class PresentationOptions
{
    [Required] public string LaunchApp { get; set; }
}