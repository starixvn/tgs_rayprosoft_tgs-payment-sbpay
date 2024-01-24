using System.Text.Json;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Tgs.Payment.Clients;
using Tgs.Payment.Protos;
using Tgs.Payment.SBPay.Client;
using Valhalla.MessageQueue;

namespace Tgs.Payment.SBPay.MessageQueue;

internal class DepositProcessor : IMessageProcessor
{
    private readonly SBPayClient _sBPayClient;
    private readonly IPaymentClient _paymentClient;
    private readonly SBPayOptions _options;

    public DepositProcessor(
        SBPayClient sBPayClient,
        IPaymentClient paymentClient,
        IOptions<SBPayOptions> optionsAccessor)
    {
        _sBPayClient = sBPayClient ?? throw new ArgumentNullException(nameof(sBPayClient));
        _paymentClient = paymentClient ?? throw new ArgumentNullException(nameof(paymentClient));
        _options = optionsAccessor.Value;
    }

    public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        var request = DepositRequest.Parser.ParseFrom(data.Span);

        var settings = JsonSerializer.Deserialize<SBPayDepositSettings>(
            request.ApplyInfo,
            SBPayUtility.SerializerOptions)!;

        var order = new DepositOrder(
            decimal.Parse(request.Amount) * settings.ExchangeRate,
            settings.FormId,
            new Uri($"{_options.CallbackBaseUrl}/deposit/callback"),
            settings.Bank);

        var depositResponse = await _sBPayClient.DepositAsync(
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

        await _paymentClient.ScheduleAsync(
            "tgs.payment.sbpay.deposit.schedule.reject",
            settings.FormId,
            DateTime.UtcNow.AddMinutes(60D),
            cancellationToken).ConfigureAwait(false);

        var msg = new DepositResponse
        {
            Result = JsonSerializer.Serialize(applyInfo, SBPayUtility.SerializerOptions)
        };

        return msg.ToByteArray();
    }
}
