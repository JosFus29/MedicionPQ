namespace MedicionPQ.Modelos.Medidores;

public class EventoConfig
{
    public int IdEvento { get; set; }
    public int NumEvento { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public int IdUnidad { get; set; }
    public bool Edo { get; set; }
}