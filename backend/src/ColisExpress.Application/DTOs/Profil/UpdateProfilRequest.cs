namespace ColisExpress.Application.DTOs.Profil;

public class UpdateProfilRequest
{
    public string Prenom { get; set; } = string.Empty;
    public string Nom { get; set; } = string.Empty;
    public string Telephone { get; set; } = string.Empty;
    public string? Adresse { get; set; }
}

public class ChangerMotDePasseRequest
{
    public string AncienMotDePasse { get; set; } = string.Empty;
    public string NouveauMotDePasse { get; set; } = string.Empty;
    public string ConfirmationNouveauMotDePasse { get; set; } = string.Empty;
}

public class ProfilStatsResponse
{
    public int EnvoisTotaux { get; init; }
    public int Livres { get; init; }
    public DateTime MembreDepuis { get; init; }
    public decimal MontantAnneeEnCours { get; init; }
    public decimal PlafondCotransportage { get; init; }
    public bool AlerteCotransportage { get; init; }
}
