using Tgs.Payment.Clients;
using Tgs.Payment.SBPay.Client;

namespace Tgs.Payment.SBPay;
public record WithdrawForm(
    Guid Id,
    string? UserId,
    UserType UserType,
    Guid BrandId,
    Guid? BrandPaymentId,
    decimal Amount,
    PaymentFlowState Status,
    Guid BankInfoId,
    SBPayWithdrawApplyInfo? ApplyInfo,
    string? PaymentReturn = null);
