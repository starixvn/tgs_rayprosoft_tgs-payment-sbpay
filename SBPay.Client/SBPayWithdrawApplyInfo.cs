namespace Tgs.Payment.SBPay.Client;
public record SBPayWithdrawApplyInfo(
    SBPayWithdrawSettings Settings,
    SBPayWithdrawResponse Response);
