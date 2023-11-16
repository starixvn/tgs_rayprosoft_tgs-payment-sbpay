using Tgs.Payment.Clients;
using Tgs.Payment.SBPay.Client;

namespace Tgs.Payment.SBPay;
public record DepositForm(
    Guid Id,
    string PlayerCode,
    Guid BrandId,
    Guid BrandPaymentId,
    decimal Amount,
    PaymentFlowState Status,
    SBPayDepositApplyInfo? ApplyInfo,
    string? PaymentReturn = null);
