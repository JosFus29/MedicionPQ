using MedicionPQ.Modelos;

namespace MedicionPQ.Services
{
    /// <summary>
    /// Define las operaciones disponibles para consultar y actualizar las variables eléctricas (Vars5M).
    /// </summary>
    public interface IVars5MService
    {
        /// <summary>Obtiene todas las variables eléctricas configuradas.</summary>
        Task<List<Vars5M>> GetAllAsync();

        /// <summary>
        /// Actualiza el estado (edo) y el número de fases de una variable existente.
        /// Devuelve la variable actualizada, o null si no existe el idVar5M indicado.
        /// </summary>
        Task<Vars5M?> UpdateAsync(int idVar5M, UpdateVars5MRequest datos);
    }
}