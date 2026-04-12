using ColisExpress.Domain.Enums;

namespace ColisExpress.Domain.Entities;

public class DocumentKyc
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TransporteurId { get; set; }
    public TypeDocument TypeDocument { get; set; }
    public string NomFichier { get; set; } = string.Empty;
    public string CheminFichier { get; set; } = string.Empty;
    public StatutKyc Statut { get; set; } = StatutKyc.EnAttente;
    public DateTime DateSoumission { get; set; } = DateTime.UtcNow;
    public DateTime? DateValidation { get; set; }
    public Guid? ValidePar { get; set; }

    public Transporteur? Transporteur { get; set; }
}
