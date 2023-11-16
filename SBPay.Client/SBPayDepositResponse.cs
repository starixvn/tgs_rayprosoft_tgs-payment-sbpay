namespace Tgs.Payment.SBPay.Client;
public record SBPayDepositResponse(
    decimal Amount,
    Guid FormId,
    Uri RedirectUrl,
    string ThirdPartyOrderNo);
