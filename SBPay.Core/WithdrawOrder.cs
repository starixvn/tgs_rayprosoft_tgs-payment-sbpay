namespace Tgs.Payment.SBPay;
public record WithdrawOrder(
    Guid FormId,
    decimal Amount,
    Uri CallbackUrl,
    string BankId,
    string BankOwner,
    string AccountNumber);
