using ColisExpress.Domain.Enums;

namespace ColisExpress.Domain.Exceptions;

public class TransitionStatutException : DomainException
{
    public StatutColis AncienStatut { get; }
    public StatutColis NouveauStatut { get; }

    public TransitionStatutException(StatutColis ancienStatut, StatutColis nouveauStatut, string raison)
        : base($"Transition interdite : {ancienStatut} → {nouveauStatut}. {raison}")
    {
        AncienStatut = ancienStatut;
        NouveauStatut = nouveauStatut;
    }
}
