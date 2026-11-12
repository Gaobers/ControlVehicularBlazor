using System.Text.Json.Serialization;

namespace ControlVehicular.DTO.EstadisticasGastos;

public sealed class EstadisticasGastosResponse
{
    [JsonRequired]
    public int CantidadGastos { get; set; }

    public DateOnly? FechaInicio { get; set; }
    public DateOnly? FechaFin { get; set; }
    public long? VehiculoId { get; set; }

    [JsonRequired]
    public List<TotalPorMonedaResponse> TotalesPorMoneda { get; set; } = [];

    [JsonRequired]
    public List<TotalPorVehiculoResponse> TotalesPorVehiculo { get; set; } = [];

    [JsonRequired]
    public List<TotalPorCategoriaResponse> TotalesPorCategoria { get; set; } = [];

    [JsonRequired]
    public List<TotalPorMesResponse> TotalesPorMes { get; set; } = [];
}

public sealed record TotalPorMonedaResponse(string Moneda, int Cantidad, decimal Total);
public sealed record TotalPorVehiculoResponse(long VehiculoId, string Moneda, int Cantidad, decimal Total);
public sealed record TotalPorCategoriaResponse(long CategoriaId, string Moneda, int Cantidad, decimal Total);
public sealed record TotalPorMesResponse(string Mes, string Moneda, int Cantidad, decimal Total);
