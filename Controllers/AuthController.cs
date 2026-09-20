using Microsoft.AspNetCore.Mvc;
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
    

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        //validamos que no vengan vacios los campos
        if (string.IsNullOrEmpty(request.Correo) || string.IsNullOrEmpty(request.Contrasena))
        {
            return BadRequest(new { message = "Correo y Contraseña son requeridos" });
        }

        // buscamos en la tabla de usuarios si existe el usuario con la cuenta y clave proporcionados
        var usuarioEnDb = _context.Usuarios.
            FirstOrDefault(u => u.correo == request.Correo && u.contrasena == request.Contrasena);

        //si se encuentra el correo y la contraseña es correcta, retornamos un mensaje de éxito
        if (usuarioEnDb != null)
        {
            if (!usuarioEnDb.edo)
            {
                return Unauthorized(new { message = "Usuario inactivo" });
            }

            return Ok(new { 
                
                message = "Inicio de sesión exitoso", 
                usuario = usuarioEnDb.idUsuario,
                correo = usuarioEnDb.correo,
                nombreUsuario = usuarioEnDb.nombreUsuario,
                //probamos que el rol se pueda enviar como string
                rol = usuarioEnDb.rol.ToString()

            });
        }
        else
        {
            //si no se encuentra el usuario o la clave es incorrecta, retornamos un mensaje de error
            return Unauthorized(new { message = "Correo o Contraseña incorrectos" });
        }
    }
}
