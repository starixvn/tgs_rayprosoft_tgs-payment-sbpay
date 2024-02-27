using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using RichardSzalay.MockHttp;
using Tgs.Payment.Protos;
using Tgs.Payment.SBPay;
using Tgs.Payment.SBPay.MessageQueue;

namespace SBPay.Core.UnitTests;

public class GetBalanceProcessorTests
{
    [Fact]
    public async Task GetBalanceProcessor_取得商戶餘額()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://tianciv310528.com");

        var fakeSBPayClient = new SBPayClient(
            httpClient,
            new SBPayHasher(NullLogger<SBPayHasher>.Instance),
            NullLogger<SBPayClient>.Instance);

        var sut = new GetBalanceProcessor(fakeSBPayClient);

        var apiToken = "recV41A3c0DamceTCn6iU4N74h9isF9c4dtreKHFSWLvKIZhlyAvzh1g4cfp";

        var request = new PaymentBalanceRequest
        {
            BrandPaymentSettings = """
				{
					"apiToken": "recV41A3c0DamceTCn6iU4N74h9isF9c4dtreKHFSWLvKIZhlyAvzh1g4cfp"
				}
				"""
        };

        _ = mockHttp
            .Expect(HttpMethod.Get, "https://tianciv310528.com/api/balance/inquiry")
            .WithHeaders(new Dictionary<string, string>
            {
                { "Accept", "application/json" },
                { "Authorization", $"Bearer {apiToken}" }
            })
            .Respond(HttpStatusCode.OK, new StringContent("""
                {
                    "data": {
                        "balance": "988400.000"
                    },
                    "success": true
                }
                """));

        // Act
        var actual = await sut.HandleAsync(
            "tgs.payment.sbpay.balance",
            request,
            null);

        // Assert
        Assert.Equal(988400M, actual.Balance.ToDecimal());
    }
}
