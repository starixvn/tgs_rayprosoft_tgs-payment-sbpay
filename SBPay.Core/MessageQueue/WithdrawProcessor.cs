using System.Text.Json;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Tgs.Payment.Clients;
using Tgs.Payment.Protos;
using Tgs.Payment.SBPay.Client;
using Valhalla.MessageQueue;

namespace Tgs.Payment.SBPay.MessageQueue;

internal class WithdrawProcessor
    : IMessageProcessor<WithdrawRequest, WithdrawResponse>
{
    private readonly SBPayClient _sBPayClient;
    private readonly IPaymentClient _paymentClient;
    private readonly SBPayOptions _options;

    public WithdrawProcessor(
        SBPayClient sBPayClient,
        IPaymentClient paymentClient,
        IOptions<SBPayOptions> optionsAccessor)
    {
        _sBPayClient = sBPayClient ?? throw new ArgumentNullException(nameof(sBPayClient));
        _paymentClient = paymentClient ?? throw new ArgumentNullException(nameof(paymentClient));
        _options = optionsAccessor.Value;
    }

    public async ValueTask<WithdrawResponse> HandleAsync(
        string subject,
        WithdrawRequest data,
        IEnumerable<MessageHeaderValue>? headerValues,
        CancellationToken cancellationToken = default)
    {
        var settings = JsonSerializer.Deserialize<SBPayWithdrawSettings>(
            data.ApplyInfo,
            SBPayUtility.SerializerOptions)!;

        var order = new WithdrawOrder(
            settings.FormId,
            decimal.Parse(data.Amount) * settings.ExchangeRate,
            new Uri($"{_options.CallbackBaseUrl}/withdraw/callback"),
            settings.BankId,
            settings.BankOwner,
            settings.AccountNumber);

        var withdrawResponse = await _sBPayClient.WithdrawAsync(
            settings.ApiToken,
            settings.NotifyToken,
            order,
            cancellationToken).ConfigureAwait(false);
        var applyInfo = new SBPayWithdrawApplyInfo(
            settings,
            new SBPayWithdrawResponse(withdrawResponse.FormId));

        await _paymentClient.SaveWithdrawFormThirdPartyOrderNoAsync(
            settings.FormId,
            withdrawResponse.ThirdPartyOrderNo,
            cancellationToken);

        return new WithdrawResponse
        {
            Result = JsonSerializer.Serialize(applyInfo, SBPayUtility.SerializerOptions)
        };
    }
}
