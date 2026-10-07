namespace MedicionPQ.Modelos
{
    /// <summary>
    /// Datos permitidos para actualizar una variable existente de Vars5M.
    /// Solo se puede modificar el estado (activo/inactivo) y el número de fases.
    /// </summary>
    public class UpdateVars5MRequest
    {
        /// <summary>Nuevo estado: true = activo, false = inactivo.</summary>
        public bool Edo { get; set; }

        /// <summary>Nuevo número de fases (1, 2 o 3).</summary>
        public byte Fases { get; set; }
    }
}