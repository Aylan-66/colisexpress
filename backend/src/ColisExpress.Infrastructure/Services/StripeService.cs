using ColisExpress.Application.Interfaces;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace ColisExpress.Infrastructure.Services;

public class StripeService : IStripeService
{
    private readonly StripeOptions _options;

    public StripeService(IOptions<StripeOptions> options)
    {
        _options = options.Value;
        if (!string.IsNullOrWhiteSpace(_options.SecretKey))
            StripeConfiguration.ApiKey = _options.SecretKey;
    }

    public async Task<StripeCheckoutSession> CreateCheckoutSessionAsync(
        Guid commandeId,
        string codeColis,
        decimal montantEuros,
        string clientEmail,
        string successUrl,
        string cancelUrl,
        CancellationToken ct = default)
    {
        var amountCents = (long)Math.Round(montantEuros * 100m);

        var options = new SessionCreateOptions
        {
            Mode = "payment",
            CustomerEmail = string.IsNullOrWhiteSpace(clientEmail) ? null : clientEmail,
            ClientReferenceId = commandeId.ToString(),
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = _options.Currency,
                        UnitAmount = amountCents,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"Colis Express — {codeColis}",
                            Description = "Envoi de colis vers le Maghreb"
                        }
                    }
                }
            },
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            Metadata = new Dictionary<string, string>
            {
                ["commandeId"] = commandeId.ToString(),
                ["codeColis"] = codeColis
            }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options, cancellationToken: ct);

        return new StripeCheckoutSession
        {
            SessionId = session.Id,
            Url = session.Url
        };
    }

    public async Task<bool> EstSessionPayeeAsync(string sessionId, CancellationToken ct = default)
    {
        var service = new SessionService();
        var session = await service.GetAsync(sessionId, cancellationToken: ct);
        return session.PaymentStatus == "paid";
    }
}
