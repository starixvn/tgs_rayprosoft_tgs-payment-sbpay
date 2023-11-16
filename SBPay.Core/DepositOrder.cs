namespace Tgs.Payment.SBPay;
public record DepositOrder(
    decimal Amount,
    Guid FormId,
    Uri CallbackUrl,
    string? Bank);
