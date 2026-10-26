namespace MedicionPQ.Modelos.Medidores;

public class ProtocoloConfig
{
    public ushort Id { get; set; }
    public ushort TipoTransporte { get; set; }
    public ushort DataPacker { get; set; }
    public bool UsarRF { get; set; }
    public bool PubTramasRxTx { get; set; }
}
