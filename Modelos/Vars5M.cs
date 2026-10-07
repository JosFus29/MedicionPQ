using System.ComponentModel.DataAnnotations;

namespace MedicionPQ.Modelos
{
    /// <summary>
    /// Representa una variable eléctrica configurada en el sistema (tabla Vars5M).
    /// Cada registro define qué variable se mide, su unidad y en cuántas fases aplica.
    /// </summary>
    public class Vars5M
    {
        /// <summary>Identificador único de la variable.</summary>
        [Key]
        public int idVar5M { get; set; }

        /// <summary>Referencia a la unidad de medida (tabla Unidades).</summary>
        public int idUnidad { get; set; }

        /// <summary>Nombre descriptivo de la variable (ej. "Energia entregada").</summary>
        public string nombre { get; set; } = string.Empty;

        /// <summary>Símbolo corto usado internamente (ej. "P5M_EE").</summary>
        public string simbolo { get; set; } = string.Empty;

        /// <summary>Código usado por la interfaz HMI para identificar la variable.</summary>
        public int codigoIHM { get; set; }

        /// <summary>Indica si la variable está activa (true) o inactiva (false).</summary>
        public bool edo { get; set; }

        /// <summary>Número de fases en que aplica la variable (1, 2 o 3).</summary>
        public byte fases { get; set; }
    }
}