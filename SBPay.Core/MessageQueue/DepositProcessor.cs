using System.Text.Json;
using Microsoft.Extensions.Options;
using Tgs.Payment.Clients;
using Tgs.Payment.Protos;
using Tgs.Payment.SBPay.Client;
using Valhalla.MessageQueue;

namespace Tgs.Payment.SBPay.MessageQueue;

internal class DepositProcessor(
    SBPayClient sBPayClient,
    IPaymentClient paymentClient,
    IOptions<SBPayOptions> optionsAccessor)
    : IMessageProcessor<DepositRequest, DepositResponse>
{
    private readonly SBPayOptions _options = optionsAccessor.Value;

    public async ValueTask<DepositResponse> HandleAsync(
        string subject,
        DepositRequest data,
        IEnumerable<MessageHeaderValue>? headerValues,
        CancellationToken cancellationToken = default)
    {
        var settings = JsonSerializer.Deserialize<SBPayDepositSettings>(
            data.ApplyInfo,
            SBPayUtility.SerializerOptions)!;

        var order = new DepositOrder(
            decimal.Parse(data.Amount) * settings.ExchangeRate,
            settings.FormId,
            new Uri($"{_options.CallbackBaseUrl}/deposit/callback"),
            settings.Bank);

        var depositResponse = await sBPayClient.DepositAsync(
            settings.ApiToken,
            order,
            cancellationToken).ConfigureAwait(false);

        var applyInfo = new SBPayDepositApplyInfo(
            settings,
            new SBPayDepositResponse(
                depositResponse.Amount,
                depositResponse.FormId,
                depositResponse.RedirectUrl,
                depositResponse.ThirdPartyOrderNo));

        await paymentClient.ScheduleAsync(
            "tgs.payment.sbpay.deposit.schedule.reject",
            settings.FormId,
            DateTime.UtcNow.AddMinutes(60D),
            cancellationToken).ConfigureAwait(false);

        return new DepositResponse
        {
            Result = JsonSerializer.Serialize(applyInfo, SBPayUtility.SerializerOptions)
        };
    }
}
