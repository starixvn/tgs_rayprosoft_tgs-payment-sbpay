using System.ComponentModel;

namespace Tgs.Payment.SBPay.Client;
public record SBPayDepositSettings(
    string ApiToken,
    string NotifyToken,
    Guid FormId,
    string? Bank,
    [property: Description("匯率轉換")]
    decimal ExchangeRate);
