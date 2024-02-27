using System.Text.Json;
using Tgs.Payment.Protos;
using Tgs.Payment.SBPay.Client;
using Valhalla.MessageQueue;

namespace Tgs.Payment.SBPay.MessageQueue;

internal class GetBalanceProcessor(
    SBPayClient sBPayClient)
    : IMessageProcessor<PaymentBalanceRequest, PaymentBalanceResponse>
{
    public async ValueTask<PaymentBalanceResponse> HandleAsync(
        string subject,
        PaymentBalanceRequest data,
        IEnumerable<MessageHeaderValue>? headerValues,
        CancellationToken cancellationToken = default)
    {
        var settings = JsonSerializer.Deserialize<BrandPaymentMerchantInfo>(
            data.BrandPaymentSettings,
            SBPayUtility.SerializerOptions)!;

        var result = await sBPayClient.QueryBalanceAsync(
            settings.ApiToken,
            cancellationToken).ConfigureAwait(false);

        return new PaymentBalanceResponse
        {
            Balance = result
        };
    }
}
