using Microsoft.AspNetCore.Mvc;
// se agrega para poder usar Entity Framework Core
using Microsoft.EntityFrameworkCore;
using MedicionPQ.Modelos;
using MedicionPQ.Data;
using Microsoft.AspNetCore.HttpLogging;

namespace MedicionPQ.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDBContex _context;

    // Inyectamos el contexto de la base de datos a través del constructor
    public AuthController(AppDBContex context)
    {
        _context = context;
    }

    // agregamos async y task para que sea asincrono y no bloquee el hilo principal
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        //validamos que no vengan vacios los campos
        if (string.IsNullOrEmpty(request.Correo) || string.IsNullOrEmpty(request.Contrasena))
        {
            return BadRequest(new { message = "Correo y Contraseña son requeridos" });
        }

        // buscamos en la tabla de usuarios si existe el usuario con la cuenta y clave proporcionados
        var usuarioEnDb = await _context.Usuarios.
            FirstOrDefaultAsync(u => u.correo == request.Correo && u.contrasena == request.Contrasena);

        //si se encuentra el correo y la contraseña es correcta, retornamos un mensaje de éxito
        if (usuarioEnDb != null)
        {
            // Validar si el usuario está activo (bit edo = 1)
            if (!usuarioEnDb.edo)
            {
                return Unauthorized(new { message = "Usuario inactivo" });
            }
            return Ok(new
            {
                message = "Inicio de sesión exitoso",
                usuario = usuarioEnDb.idUsuario,
                correo = usuarioEnDb.correo,
                nombreUsuario = usuarioEnDb.nombreUsuario,
                //probamos que el rol se pueda enviar como string (si lo mandamos como entero, el cliente no lo va a entender)
                rol = usuarioEnDb.rol.ToString()

            });

        }
            //si no se encuentra el usuario o la clave es incorrecta, retornamos un mensaje de error
            return Unauthorized(new { message = "Correo o Contraseña incorrectos" });
    }
}
