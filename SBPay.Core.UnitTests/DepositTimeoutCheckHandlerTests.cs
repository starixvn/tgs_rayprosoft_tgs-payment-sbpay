using System.Text.Json;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using RichardSzalay.MockHttp;
using Tgs.Payment.Clients;
using Tgs.Payment.Protos;
using Tgs.Payment.SBPay;
using Tgs.Payment.SBPay.Client;
using Tgs.Payment.SBPay.MessageQueue;

namespace SBPay.Core.UnitTests;

public class DepositTimeoutCheckHandlerTests
{
    [Fact]
    public async Task 檢查已經申請六十分鐘的單子是否已經完成()
    {
        // Arrange
        var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();

        var mockHttp = new MockHttpMessageHandler();

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://tianciv310528.com");

        var fakeSBPayClient = new SBPayClient(httpClient, new SBPayHasher(NullLogger<SBPayHasher>.Instance), NullLogger<SBPayClient>.Instance);

        var fakePaymentClient = Substitute.For<IPaymentClient>();

        var sut = new DepositTimeoutCheckHandler(fakePaymentServiceClient, fakeSBPayClient, fakePaymentClient, NullLogger<DepositTimeoutCheckHandler>.Instance);

        var formId = Guid.Parse("0ff96c9d-d3f7-467f-90d2-9917820e89bf");
        var brandId = Guid.Parse("4e86719f-243f-46f6-b010-5c758b7285af");
        var request = new ScheduleRequest
        {
            FormId = formId,
            Subject = "tgs.payment.sbpay.deposit.schedule.reject",
            ScheduleTime = Timestamp.FromDateTime(DateTime.UtcNow)
        };

        var applyInfo = new SBPayDepositApplyInfo(
            new SBPayDepositSettings(
                "euIAhKfGK8oAaXIGgfhDSDWflKO003zz2tQl90jb1vuw6vz83CVxkCoptINt",
                "8E4E8xglfyfz2xCOgDawAYWQuertBteQ06mA9V9IcLafROlM186DYHPtxNEj",
                formId,
                null,
                ExchangeRate: 1m),
            new SBPayDepositResponse(
                200000m,
                formId,
                new Uri("https://tianciv310528.com/r/t/e41840c9-a792-482f-ab4d-b0dca0489757"),
                "e41840c9-a792-482f-ab4d-b0dca0489757"));

        _ = fakePaymentServiceClient.GetDepositFormAsync(Arg.Is(request.FormId.ToGuid()))
            .Returns(new DepositFormInfo
            {
                Id = formId,
                Amount = 200000m,
                BrandId = brandId,
                ApplyInfo = JsonSerializer.Serialize(applyInfo, SBPayUtility.SerializerOptions)
            });

        _ = mockHttp.When($"https://tianciv310528.com/api/transaction/{formId}")
            .Respond(new StringContent("""
				{
				    "data": {
				        "trade_no": "e41840c9-a792-482f-ab4d-b0dca0489757",
				        "out_trade_no": "0ff96c9d-d3f7-467f-90d2-9917820e89bf",
				        "amount": 200000,
				        "request_amount": 200000,
				        "state": "timeout"
				    },
				    "success": true
				}
				"""));

        // Act
        await sut.HandleAsync($"tgs.payment.sbpay.deposit.schedule.reject", request, null);

        // Assert
        _ = fakePaymentClient
            .Received(1)
            .RejectDepositAsync(Arg.Is(formId), Arg.Is(brandId));
    }

    [Fact]
    public async Task 當三方代收結果為processing時會繼續計時詢問()
    {
        // Arrange
        var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();

        var mockHttp = new MockHttpMessageHandler();

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://tianciv310528.com");

        var fakeSBPayClient = new SBPayClient(httpClient, new SBPayHasher(NullLogger<SBPayHasher>.Instance), NullLogger<SBPayClient>.Instance);

        var fakePaymentClient = Substitute.For<IPaymentClient>();

        var sut = new DepositTimeoutCheckHandler(fakePaymentServiceClient, fakeSBPayClient, fakePaymentClient, NullLogger<DepositTimeoutCheckHandler>.Instance);

        var formId = Guid.Parse("d083e4de-cc49-48ea-bcc5-c23d5edef841");
        var brandId = Guid.Parse("4e86719f-243f-46f6-b010-5c758b7285af");
        var request = new ScheduleRequest
        {
            FormId = formId,
            Subject = "tgs.payment.sbpay.deposit.schedule.reject",
            ScheduleTime = Timestamp.FromDateTime(DateTime.UtcNow)
        };

        var applyInfo = new SBPayDepositApplyInfo(
            new SBPayDepositSettings(
                "euIAhKfGK8oAaXIGgfhDSDWflKO003zz2tQl90jb1vuw6vz83CVxkCoptINt",
                "8E4E8xglfyfz2xCOgDawAYWQuertBteQ06mA9V9IcLafROlM186DYHPtxNEj",
                formId,
                null, 
                ExchangeRate: 1m),
            new SBPayDepositResponse(
                200000m,
                formId,
                new Uri("https://tianciv310528.com/r/t/59adaf4e-b2c9-4499-858d-e34835531845"),
                "59adaf4e-b2c9-4499-858d-e34835531845"));

        _ = fakePaymentServiceClient.GetDepositFormAsync(Arg.Is(request.FormId.ToGuid()))
            .Returns(new DepositFormInfo
            {
                Id = formId,
                Amount = 200000m,
                BrandId = brandId,
                ApplyInfo = JsonSerializer.Serialize(applyInfo, SBPayUtility.SerializerOptions)
            });

        _ = mockHttp.When($"https://tianciv310528.com/api/transaction/{formId}")
            .Respond(new StringContent("""
				{
				    "data": {
				        "trade_no": "59adaf4e-b2c9-4499-858d-e34835531845",
				        "out_trade_no": "d083e4de-cc49-48ea-bcc5-c23d5edef841",
				        "amount": 200000,
				        "request_amount": 200000,
				        "state": "processing"
				    },
				    "success": true
				}
				"""));

        // Act
        await sut.HandleAsync($"tgs.payment.sbpay.deposit.schedule.reject", request, null);

        // Assert
        _ = fakePaymentClient.Received(1)
        .ScheduleAsync(
                Arg.Is("tgs.payment.sbpay.deposit.schedule.reject"),
                Arg.Is(request.FormId.ToGuid()),
                Arg.Any<DateTime>());
    }
}
