using Microsoft.AspNetCore.Mvc;
using MedicionPQ.Modelos;
using Microsoft.AspNetCore.HttpLogging;

namespace MedicionPQ.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        //validacion de los campos
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
        {
            return BadRequest("El correo electrónico y la contraseña son requeridos.");
        }

        //simulación de autenticación
        if (request.Email == "admin@example.com" && request.Password == "12345678")
        {
            return Ok(new
            {
                mensaje = "Inicio de sesion exitoso",
                usuario = request.Email,
                rol = "Administrador"
            });
        }

        //si las credenciales son incorrectas
        return Unauthorized(new { mensaje = "Credenciales incorrectas." });
    }
}
