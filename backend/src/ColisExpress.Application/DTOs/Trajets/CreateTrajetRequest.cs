using ColisExpress.Domain.Enums;

namespace ColisExpress.Application.DTOs.Trajets;

public class RegisterTransporteurRequest
{
    public string Prenom { get; set; } = string.Empty;
    public string Nom { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telephone { get; set; } = string.Empty;
    public string MotDePasse { get; set; } = string.Empty;
    public string ConfirmationMotDePasse { get; set; } = string.Empty;
    public string TypeVehicule { get; set; } = string.Empty;
    public string CorridorsActifs { get; set; } = string.Empty;
}

public class UploadDocumentKycRequest
{
    public Guid TransporteurId { get; set; }
    public TypeDocument TypeDocument { get; set; }
    public string NomFichier { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Contenu { get; set; } = Array.Empty<byte>();
}

public class TransporteurDashboardResponse
{
    public Guid TransporteurId { get; init; }
    public StatutKyc StatutKyc { get; init; }
    public IReadOnlyList<DocumentKycItem> Documents { get; init; } = Array.Empty<DocumentKycItem>();
    public bool PeutPublierOffres { get; init; }
}

public class DocumentKycItem
{
    public Guid Id { get; init; }
    public TypeDocument TypeDocument { get; init; }
    public string NomFichier { get; init; } = string.Empty;
    public StatutKyc Statut { get; init; }
    public DateTime DateSoumission { get; init; }
}
