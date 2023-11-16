namespace Tgs.Payment.SBPay;
public record SBPayClientWithdrawResponse(
    Guid FormId,
    string ThirdPartyOrderNo,
    decimal Amount);
