using System.ComponentModel.DataAnnotations;

namespace SimpleExampleInvoice.Models;

public class User
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El Usuario es obligatorio")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    public string Password { get; set; } = string.Empty;
}
