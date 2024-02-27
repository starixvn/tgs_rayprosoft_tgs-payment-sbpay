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

public class DepositProcessorTests
{
    [Fact]
    public async Task 儲值要求處理_純卡網關()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://tianciv310528.com");

        var fakeSBPayClient = new SBPayClient(
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
        var formId = Guid.Parse("86365b8e-8368-11ee-8aa3-0242ac120006");
        var apiToken = "X9VmKmXXWn3xviG1XojhW2LYzn72e0B3ockBEsYSdXrjzmaoufpg4fVUnQy4";
        var notifyToken = "eqfB75dajWSyWYQ6x8D0Q1oKKsUkTPnA208VZtyQVBIJX1wqUIArA3MOWU5y";

        var sut = new DepositProcessor(
            fakeSBPayClient,
            fakePaymentClient,
            new OptionsWrapper<SBPayOptions>(options));

        var applySettings = new SBPayDepositSettings(
            apiToken,
            notifyToken,
            formId,
            null,
            ExchangeRate: 1m);

        var request = new DepositRequest
        {
            Amount = "200000",
            ApplyInfo = JsonSerializer.Serialize(applySettings, SBPayUtility.SerializerOptions)
        };

        _ = mockHttp.Expect(HttpMethod.Post, "https://tianciv310528.com/api/transaction")
            .WithHeaders(new Dictionary<string, string>
            {
                { "Accept", "application/json" },
                { "Authorization", $"Bearer {apiToken}" }
            })
            .With(req => req.Content?.Headers.ContentType?.MediaType == "application/json")
            .Respond(HttpStatusCode.OK, new StringContent("""
                {
                    "data": {
                        "trade_no": "1eaf0f7a-a8d3-4567-a689-fcf0405fbb11",
                        "out_trade_no": "86365b8e-8368-11ee-8aa3-0242ac120006",
                        "amount": 200000,
                        "uri": "https://tianciv310528.com/r/t/1eaf0f7a-a8d3-4567-a689-fcf0405fbb11",
                        "bank_owner": null,
                        "qrcode": ""
                    },
                    "success": true
                }
                """));

        // Act
        var actual = await sut.HandleAsync(
            "tgs.payment.sbpay.deposit",
            request,
            null);

        var applyInfo = JsonSerializer.Deserialize<SBPayDepositApplyInfo>(actual.Result, SBPayUtility.SerializerOptions);

        // Assert
        _ = fakePaymentClient.Received(1)
            .ScheduleAsync(
                Arg.Is("tgs.payment.sbpay.deposit.schedule.reject"),
                Arg.Is(applySettings.FormId),
                Arg.Any<DateTime>());
        Assert.NotNull(applyInfo);
        Assert.Equal(applySettings, applyInfo.Settings);
        Assert.Equal(
            new SBPayDepositResponse(
                200000m,
                applySettings.FormId,
                new Uri("https://tianciv310528.com/r/t/1eaf0f7a-a8d3-4567-a689-fcf0405fbb11"),
                "1eaf0f7a-a8d3-4567-a689-fcf0405fbb11"),
            applyInfo.Response);
    }

    [Fact]
    public async Task 儲值要求處理_純卡網關_匯率轉換()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://tianciv310528.com");

        var fakeSBPayClient = new SBPayClient(
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
        var formId = Guid.Parse("86365b8e-8368-11ee-8aa3-0242ac120006");
        var apiToken = "X9VmKmXXWn3xviG1XojhW2LYzn72e0B3ockBEsYSdXrjzmaoufpg4fVUnQy4";
        var notifyToken = "eqfB75dajWSyWYQ6x8D0Q1oKKsUkTPnA208VZtyQVBIJX1wqUIArA3MOWU5y";

        var sut = new DepositProcessor(
            fakeSBPayClient,
            fakePaymentClient,
            new OptionsWrapper<SBPayOptions>(options));

        var applySettings = new SBPayDepositSettings(
            apiToken,
            notifyToken,
            formId,
            null,
            ExchangeRate: 1000m);

        var request = new DepositRequest
        {
            Amount = "200",
            ApplyInfo = JsonSerializer.Serialize(applySettings, SBPayUtility.SerializerOptions)
        };

        _ = mockHttp.Expect(HttpMethod.Post, "https://tianciv310528.com/api/transaction")
            .WithHeaders(new Dictionary<string, string>
            {
                { "Accept", "application/json" },
                { "Authorization", $"Bearer {apiToken}" }
            })
            .With(req => req.Content?.Headers.ContentType?.MediaType == "application/json")
            .Respond(HttpStatusCode.OK, new StringContent("""
                {
                    "data": {
                        "trade_no": "1eaf0f7a-a8d3-4567-a689-fcf0405fbb11",
                        "out_trade_no": "86365b8e-8368-11ee-8aa3-0242ac120006",
                        "amount": 200000,
                        "uri": "https://tianciv310528.com/r/t/1eaf0f7a-a8d3-4567-a689-fcf0405fbb11",
                        "bank_owner": null,
                        "qrcode": ""
                    },
                    "success": true
                }
                """));

        // Act
        var actual = await sut.HandleAsync("tgs.payment.sbpay.deposit",
            request,
            null);
        
        var applyInfo = JsonSerializer.Deserialize<SBPayDepositApplyInfo>(actual.Result, SBPayUtility.SerializerOptions);

        // Assert
        _ = fakePaymentClient.Received(1)
            .ScheduleAsync(
                Arg.Is("tgs.payment.sbpay.deposit.schedule.reject"),
                Arg.Is(applySettings.FormId),
                Arg.Any<DateTime>());
        Assert.NotNull(applyInfo);
        Assert.Equal(applySettings, applyInfo.Settings);
        Assert.Equal(
            new SBPayDepositResponse(
                200000m,
                applySettings.FormId,
                new Uri("https://tianciv310528.com/r/t/1eaf0f7a-a8d3-4567-a689-fcf0405fbb11"),
                "1eaf0f7a-a8d3-4567-a689-fcf0405fbb11"),
            applyInfo.Response);
    }
}
