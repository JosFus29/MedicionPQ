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
        if (string.IsNullOrEmpty(request.Cuenta) || string.IsNullOrEmpty(request.Clave))
        {
            return BadRequest(new { message = "Cuenta y Clave son requeridos" });
        }

        // buscamos en la tabla de usuarios si existe el usuario con la cuenta y clave proporcionados
        var usuarioEnDb = _context.Usuarios.FirstOrDefault(u => u.CUENTA == request.Cuenta && u.CLAVE == request.Clave);

        //si se encuentra el usuario y la clave es correcta, retornamos un mensaje de éxito
        if (usuarioEnDb != null)
        {
            return Ok(new { 
                
                message = "Inicio de sesión exitoso", 
                usuario = usuarioEnDb.id,
                cuenta = usuarioEnDb.CUENTA,
                rol = usuarioEnDb.ROL
            });
        }
        else
        {
            //si no se encuentra el usuario o la clave es incorrecta, retornamos un mensaje de error
            return Unauthorized(new { message = "Cuenta o Clave incorrectos" });
        }
    }
}
