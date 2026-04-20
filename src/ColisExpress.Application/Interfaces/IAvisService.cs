namespace ColisExpress.Application.Interfaces;

public class CreateAvisRequest
{
    public Guid CommandeId { get; set; }
    public Guid ClientId { get; set; }
    public int Note { get; set; }
    public string? Commentaire { get; set; }
}

public class AvisResponse
{
    public Guid Id { get; init; }
    public int Note { get; init; }
    public string? Commentaire { get; init; }
    public string NomClient { get; init; } = string.Empty;
    public DateTime DateCreation { get; init; }
}

public interface IAvisService
{
    Task<OperationResult> CreateAsync(CreateAvisRequest request, CancellationToken ct = default);
    Task<AvisResponse?> GetByCommandeIdAsync(Guid commandeId, CancellationToken ct = default);
    Task<bool> AvisExisteAsync(Guid commandeId, CancellationToken ct = default);
}
