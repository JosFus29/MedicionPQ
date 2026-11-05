namespace MedicionPQ.Modelos.Medidores;

using System.Collections.Generic;

public class RootPuntosIOConfig
{
    public List<PuntoIOConfig> Inst { get; set; }
    public List<PuntoIOConfig> UV5M { get; set; }
    public List<PuntoIOConfig> Consumos { get; set; }
    public List<PuntoIOConfig> SetPoints { get; set; }
    public List<PuntoIOConfig> CalPot { get; set; }
    public List<PuntoIOConfig> Armonicos { get; set; }
    public List<PuntoIOConfig> Varios { get; set; }
    public List<PuntoIOConfig> Calib { get; set; }
}
