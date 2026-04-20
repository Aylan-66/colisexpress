namespace ColisExpress.Application.DTOs.Offres;

public enum TriOffres
{
    Prix,
    Note,
    Rapidite
}

public class RechercheOffreRequest
{
    public string VilleDepart { get; set; } = string.Empty;
    public string VilleArrivee { get; set; } = string.Empty;
    public DateTime DateDepart { get; set; }
    public decimal Poids { get; set; }
    public bool Fragile { get; set; }
    public bool Urgent { get; set; }
    public bool Assurance { get; set; }
    public TriOffres Tri { get; set; } = TriOffres.Prix;
}
