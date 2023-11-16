namespace Tgs.Payment.SBPay.Client;
public record SBPayDepositSettings(
    string ApiToken,
    string NotifyToken,
    Guid FormId,
    string? Bank);
