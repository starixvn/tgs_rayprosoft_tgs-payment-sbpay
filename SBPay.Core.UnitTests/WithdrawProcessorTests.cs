using System.Net;
using System.Text.Json;
using Google.Protobuf;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using RichardSzalay.MockHttp;
using Tgs.Payment.Clients;
using Tgs.Payment.Protos;
using Tgs.Payment.SBPay;
using Tgs.Payment.SBPay.Client;
using Tgs.Payment.SBPay.MessageQueue;

namespace SBPay.Core.UnitTests;

public class WithdrawProcessorTests
{
    [Fact]
    public async Task 處理提款要求_銀行()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://tianciv310528.com");

        var fakeGCPayClient = new SBPayClient(
            httpClient,
            new SBPayHasher(NullLogger<SBPayHasher>.Instance),
            NullLogger<SBPayClient>.Instance);

        var fakePaymentClient = Substitute.For<IPaymentClient>();
        var options = new SBPayOptions
        {
            CallbackBaseUrl = new Uri("https://dev-payment.rpgp.cc/sbpay"),
            PaymentApiUrl = new Uri("https://tianciv310528.com"),
            PaymentServiceBaseUrl = new Uri("http://localhost:5541")
        };

        var sut = new WithdrawProcessor(
            fakeGCPayClient,
            fakePaymentClient,
            new OptionsWrapper<SBPayOptions>(options));

        var formId = Guid.Parse("48f5cb14-838c-11ee-8aa3-0242ac120006");
        var apiToken = "recV41A3c0DamceTCn6iU4N74h9isF9c4dtreKHFSWLvKIZhlyAvzh1g4cfp";
        var notifyToken = "Iklv4KrHlyus5tBO6TXkgZaonYE4OIV0WTBtgKkTRdWmB51chdhkjHF7kb3A";

        var applySettings = new SBPayWithdrawSettings(
            apiToken,
            notifyToken,
            formId,
            BankId: "ACB",
            BankOwner: "王小明",
            AccountNumber: "6212262000000000001");

        var request = new WithdrawRequest
        {
            Amount = "500",
            ApplyInfo = JsonSerializer.Serialize(applySettings, SBPayUtility.SerializerOptions)
        };

        _ = mockHttp.Expect(HttpMethod.Post, "https://tianciv310528.com/api/payment")
            .WithHeaders(new Dictionary<string, string>
            {
                { "Accept", "application/json" },
                { "Authorization", $"Bearer {apiToken}" }
            })
            .With(req => req.Content?.Headers.ContentType?.MediaType == "application/json")
            .Respond(HttpStatusCode.OK, new StringContent("""
                {
                    "data": {
                        "trade_no": "887b4e0d-2d10-40a4-9266-6930276a0bd9",
                        "out_trade_no": "48f5cb14-838c-11ee-8aa3-0242ac120006",
                        "bank_id": "ACB",
                        "bank_owner": "王小明",
                        "account_number": "6212262000000000001",
                        "amount": 500,
                        "callback_url": "https://dev-payment.rpgp.cc/sbpay/withdraw/callback",
                        "state": "new"
                    },
                    "success": true
                }
                """));

        // Act
        var actual = await sut.HandleAsync(request.ToByteArray());
        var response = WithdrawResponse.Parser.ParseFrom(actual.Span);
        var applyInfo = JsonSerializer.Deserialize<SBPayWithdrawApplyInfo>(response.Result, SBPayUtility.SerializerOptions);

        // Assert
        _ = fakePaymentClient.Received(1)
            .SaveWithdrawFormThirdPartyOrderNoAsync(
                Arg.Is(applySettings.FormId),
                Arg.Is("887b4e0d-2d10-40a4-9266-6930276a0bd9"));
        Assert.NotNull(applyInfo);
        Assert.Equal(applySettings, applyInfo.Settings);
    }
}
