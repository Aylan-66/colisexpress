namespace ColisExpress.Application.Interfaces;

public class StripeOptions
{
    public string PublishableKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Currency { get; set; } = "eur";
}

public class StripeCheckoutSession
{
    public string SessionId { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
}

public interface IStripeService
{
    Task<StripeCheckoutSession> CreateCheckoutSessionAsync(
        Guid commandeId,
        string codeColis,
        decimal montantEuros,
        string clientEmail,
        string successUrl,
        string cancelUrl,
        CancellationToken ct = default);

    Task<bool> EstSessionPayeeAsync(string sessionId, CancellationToken ct = default);
}
