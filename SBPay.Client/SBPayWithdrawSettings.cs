namespace Tgs.Payment.SBPay.Client;
public record SBPayWithdrawSettings(
    string ApiToken,
    string NotifyToken,
    Guid FormId,
    string BankId,
    string BankOwner,
    string AccountNumber);
