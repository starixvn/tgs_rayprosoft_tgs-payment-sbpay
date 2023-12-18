using System.ComponentModel;

namespace Tgs.Payment.SBPay.Client;
public record SBPayWithdrawSettings(
    string ApiToken,
    string NotifyToken,
    Guid FormId,
    string BankId,
    string BankOwner,
    string AccountNumber,
    [property: Description("匯率轉換")]
    decimal ExchangeRate);
