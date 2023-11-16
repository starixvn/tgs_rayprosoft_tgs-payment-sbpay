using System.Text.Json;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Tgs.Payment.Clients;
using Tgs.Payment.Protos;
using Tgs.Payment.SBPay.Client;
using Valhalla.MessageQueue;

namespace Tgs.Payment.SBPay.MessageQueue;

internal class WithdrawProcessor : IMessageProcessor
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

    public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        var request = WithdrawRequest.Parser.ParseFrom(data.Span);
        var settings = JsonSerializer.Deserialize<SBPayWithdrawSettings>(request.ApplyInfo, SBPayUtility.SerializerOptions)!;

        var order = new WithdrawOrder(
            settings.FormId,
            decimal.Parse(request.Amount),
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

        var msg = new WithdrawResponse
        {
            Result = JsonSerializer.Serialize(applyInfo, SBPayUtility.SerializerOptions)
        };

        return msg.ToByteArray();
    }
}
