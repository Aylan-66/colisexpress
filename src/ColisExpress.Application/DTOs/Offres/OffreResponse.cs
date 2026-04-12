namespace ColisExpress.Application.DTOs.Offres;

public class OffreResponse
{
    public Guid TrajetId { get; init; }
    public Guid TransporteurId { get; init; }
    public string NomTransporteur { get; init; } = string.Empty;
    public string Initiales { get; init; } = "";
    public decimal NoteMoyenne { get; init; }
    public int NombreAvis { get; init; }
    public string VilleDepart { get; init; } = string.Empty;
    public string VilleArrivee { get; init; } = string.Empty;
    public DateTime DateDepart { get; init; }
    public DateTime DateEstimeeArrivee { get; init; }
    public int CapaciteRestante { get; init; }
    public decimal CapaciteMaxPoids { get; init; }
    public string TypeVehicule { get; init; } = string.Empty;
    public decimal Prix { get; init; }
    public decimal PoidsRecherche { get; init; }
}
