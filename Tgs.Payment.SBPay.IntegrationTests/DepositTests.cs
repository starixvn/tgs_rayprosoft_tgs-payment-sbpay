using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Tgs.Payment.Clients;
using Tgs.Payment.SBPay.Client;
using Xunit.Abstractions;

namespace Tgs.Payment.SBPay.IntegrationTests;
public class DepositTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public DepositTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper ?? throw new ArgumentNullException(nameof(testOutputHelper));
    }
    [Fact]
    public async Task 代收回調_訂單狀態成功()
    {
        var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
        var fakePaymentClient = Substitute.For<IPaymentClient>();

        await using var web = SBPayTestHost.Factory
            .WithWebHostBuilder(hostBuilder => hostBuilder
                .ConfigureLogging(logging => logging.AddXUnit(_testOutputHelper))
                .ConfigureServices(services => services
                    .AddSingleton(fakePaymentServiceClient)
                    .AddSingleton(fakePaymentClient)));

        var httpClient = web.CreateClient();

        var formId = Guid.Parse("86365b8e-8368-11ee-8aa3-0242ac120006");
        var apiToken = "X9VmKmXXWn3xviG1XojhW2LYzn72e0B3ockBEsYSdXrjzmaoufpg4fVUnQy4";
        var notifyToken = "eqfB75dajWSyWYQ6x8D0Q1oKKsUkTPnA208VZtyQVBIJX1wqUIArA3MOWU5y";

        var request = """
            {
                "trade_no": "1eaf0f7a-a8d3-4567-a689-fcf0405fbb11",
                "request_amount": "200000",
                "amount": "200000",
                "out_trade_no": "86365b8e-8368-11ee-8aa3-0242ac120006",
                "state": "completed",                
                "sign": "c9962ebb037e483bd85a834e228cc56d",
                "callback_url": "https://dev-payment.rpgp.cc/sbpay/deposit/callback"
            }
            """;

        var content = new StringContent(request, Encoding.UTF8, new MediaTypeHeaderValue(MediaTypeNames.Application.Json));

        var applyInfo = new SBPayDepositApplyInfo(
            new SBPayDepositSettings(
                apiToken,
                notifyToken,
                formId,
                null),
             new SBPayDepositResponse(
                200000m,
                formId,
                new Uri("https://tianciv310528.com/r/t/1eaf0f7a-a8d3-4567-a689-fcf0405fbb11"),
                "1eaf0f7a-a8d3-4567-a689-fcf0405fbb11"));

        _ = fakePaymentServiceClient.GetDepositFormAsync(Arg.Is(formId), Arg.Any<CancellationToken>())
            .Returns(new DepositFormInfo
            {
                Id = formId,
                BrandId = Guid.NewGuid(),
                Status = PaymentFlowState.WaitPayment,
                Amount = 200000m,
                ThirdPartyOrderNo = "1eaf0f7a-a8d3-4567-a689-fcf0405fbb11",
                ApplyInfo = JsonSerializer.Serialize(applyInfo, SBPayUtility.SerializerOptions)
            });

        var response = await httpClient.PostAsync("/deposit/callback", content);

        var actual = await response.Content.ReadAsStringAsync();

        Assert.Equal("ok", actual);

        _ = fakePaymentServiceClient.Received(1)
            .SubmitDepositFormStatusAsync(
                Arg.Is(formId),
                paymentReturn: Arg.Any<string>(),
                cancellationToken: Arg.Any<CancellationToken>());

        _ = fakePaymentClient.Received(1)
            .ApproveDepositAsync(
                Arg.Is(formId),
                Arg.Any<Guid>(),
                Arg.Is(applyInfo.Response.Amount),
                Arg.Any<CancellationToken>());
    }
}
