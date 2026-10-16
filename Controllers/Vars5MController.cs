using MedicionPQ.Modelos;
using MedicionPQ.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicionPQ.Controllers
{
    /// <summary>
    /// Expone operaciones de consulta y actualización sobre las variables eléctricas (Vars5M).
    /// Solo usuarios autenticados pueden acceder a estos endpoints.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class Vars5MController : ControllerBase
    {
        private readonly IVars5MService _vars5MService;

        /// <summary>Recibe el servicio de variables eléctricas por inyección de dependencias.</summary>
        public Vars5MController(IVars5MService vars5MService)
        {
            _vars5MService = vars5MService;
        }

        /// <summary>
        /// Obtiene todas las variables eléctricas configuradas (Vars5M).
        /// Disponible para cualquier usuario autenticado.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<Vars5M>>> GetAll()
        {
            var variables = await _vars5MService.GetAllAsync();
            return Ok(variables);
        }

        /// <summary>
        /// Actualiza el estado (edo) y el número de fases de una variable existente.
        /// Solo usuarios con rol Administrador pueden modificar esta configuración.
        /// </summary>
        /// <param name="id">Identificador de la variable (idVar5M) a actualizar.</param>
        /// <param name="datos">Nuevos valores de edo y fases.</param>
        [HttpPut("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<Vars5M>> Update(int id, [FromBody] UpdateVars5MRequest datos)
        {
            var actualizado = await _vars5MService.UpdateAsync(id, datos);

            if (actualizado is null)
                return NotFound($"No existe una variable con idVar5M = {id}");

            return Ok(actualizado);
        }
    }
}