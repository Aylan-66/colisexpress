using ColisExpress.Domain.Enums;

namespace ColisExpress.Application.DTOs.Admin;

public class DashboardResponse
{
    public int ColisCeMois { get; init; }
    public int ColisLivres { get; init; }
    public int TransporteursActifs { get; init; }
    public int Incidents { get; init; }
    public int TransporteursEnAttenteKyc { get; init; }
    public IReadOnlyList<CommandeRecenteItem> CommandesRecentes { get; init; } = Array.Empty<CommandeRecenteItem>();
    public IReadOnlyList<TransporteurListItem> TransporteursAValider { get; init; } = Array.Empty<TransporteurListItem>();
}

public class CommandeRecenteItem
{
    public Guid Id { get; init; }
    public string CodeColis { get; init; } = string.Empty;
    public string Trajet { get; init; } = string.Empty;
    public string Client { get; init; } = string.Empty;
    public StatutColis Statut { get; init; }
    public decimal Total { get; init; }
    public DateTime DateCreation { get; init; }
}

public class UtilisateurListItem
{
    public Guid Id { get; init; }
    public string Nom { get; init; } = string.Empty;
    public string Prenom { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public RoleUtilisateur Role { get; init; }
    public StatutCompte StatutCompte { get; init; }
    public DateTime DateCreation { get; init; }
}

public class TransporteurListItem
{
    public Guid Id { get; init; }
    public string Nom { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Telephone { get; init; } = string.Empty;
    public StatutKyc StatutKyc { get; init; }
    public decimal NoteMoyenne { get; init; }
    public int NombreAvis { get; init; }
    public string? TypeVehicule { get; init; }
}

public class PointRelaisListItem
{
    public Guid Id { get; init; }
    public string NomRelais { get; init; } = string.Empty;
    public string Ville { get; init; } = string.Empty;
    public string Pays { get; init; } = string.Empty;
    public string Telephone { get; init; } = string.Empty;
    public bool EstActif { get; init; }
}

public class CommandeAdminListItem
{
    public Guid Id { get; init; }
    public string CodeColis { get; init; } = string.Empty;
    public string Trajet { get; init; } = string.Empty;
    public string Client { get; init; } = string.Empty;
    public string Transporteur { get; init; } = string.Empty;
    public StatutColis StatutColis { get; init; }
    public StatutReglement StatutReglement { get; init; }
    public decimal Total { get; init; }
    public DateTime DateCreation { get; init; }
}

public class PaiementAdminListItem
{
    public Guid Id { get; init; }
    public string CodeColis { get; init; } = string.Empty;
    public string Client { get; init; } = string.Empty;
    public ModeReglement Mode { get; init; }
    public decimal Montant { get; init; }
    public StatutReglement Statut { get; init; }
    public DateTime DateCreation { get; init; }
}

public class KycDecisionRequest
{
    public Guid TransporteurId { get; set; }
    public bool Approuver { get; set; }
    public string? Motif { get; set; }
}
