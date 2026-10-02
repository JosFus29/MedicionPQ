using MedicionPQ.Modelos;

namespace MedicionPQ.Services;

/// <summary>Operaciones para registrar y actualizar medidores QP.</summary>
public interface IMedidorQPService
{
    /// <summary>Guarda un medidor si su controlador existe y el estado es válido.</summary>
    Task<(MedidorQP? Medidor, string? Error)> CreateAsync(MedidorQPRequest request);

    /// <summary>Reemplaza los campos editables de un medidor, si existe.</summary>
    Task<(MedidorQP? Medidor, string? Error)> UpdateAsync(int id, MedidorQPRequest request);
}
