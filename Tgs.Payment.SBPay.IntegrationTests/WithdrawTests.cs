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
public class WithdrawTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public WithdrawTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper ?? throw new ArgumentNullException(nameof(testOutputHelper));
    }

    [Fact]
    public async Task 代付回調_訂單狀態成功()
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

        var formId = Guid.Parse("4d9a37f0-84f3-11ee-816e-0242ac120003");
        var brandId = Guid.NewGuid();
        var apiToken = "recV41A3c0DamceTCn6iU4N74h9isF9c4dtreKHFSWLvKIZhlyAvzh1g4cfp";
        var notifyToken = "Iklv4KrHlyus5tBO6TXkgZaonYE4OIV0WTBtgKkTRdWmB51chdhkjHF7kb3A";

        var settings = new SBPayWithdrawSettings(
            apiToken,
            notifyToken,
            formId,
            "ACB",
            "王小明",
            "6212262000000000001");
        var withdrawResponse = new SBPayWithdrawResponse(formId);

        var applyInfo = new SBPayWithdrawApplyInfo(settings, withdrawResponse);

        _ = fakePaymentServiceClient.GetWithdrawFormAsync(
                Arg.Is(formId),
                Arg.Any<CancellationToken>())
            .Returns(new WithdrawFormInfo
            {
                Id = formId,
                BrandId = brandId,
                Status = PaymentFlowState.Approved,
                ApplyInfo = JsonSerializer.Serialize(applyInfo, SBPayUtility.SerializerOptions)
            });

        var request = """
            {
                "trade_no": "1d786b53-b17d-4aa3-8ad0-bc9a57e100fe",
                "amount": 500,
                "out_trade_no": "4d9a37f0-84f3-11ee-816e-0242ac120003",
                "state": "completed",
                "sign": "76a55d5636b9285cdc10cf5cb33811d1"
            }
            """;

        var content = new StringContent(request, Encoding.UTF8, new MediaTypeHeaderValue(MediaTypeNames.Application.Json));

        var response = await httpClient.PostAsync("/withdraw/callback", content);

        var actual = await response.Content.ReadAsStringAsync();

        Assert.Equal("ok", actual);
        _ = fakePaymentServiceClient.Received(1)
            .SubmitWithdrawFormStatusAsync(
                Arg.Is(formId),
                paymentReturn: Arg.Any<string>(),
                cancellationToken: Arg.Any<CancellationToken>());

        _ = fakePaymentClient.Received(1)
            .PaidWithdrawAsync(
                Arg.Is(formId),
                Arg.Is(brandId),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task 代付回調_訂單狀態失敗()
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

        var formId = Guid.Parse("2ddf9804-84f5-11ee-816e-0242ac120003");
        var brandId = Guid.NewGuid();
        var apiToken = "recV41A3c0DamceTCn6iU4N74h9isF9c4dtreKHFSWLvKIZhlyAvzh1g4cfp";
        var notifyToken = "Iklv4KrHlyus5tBO6TXkgZaonYE4OIV0WTBtgKkTRdWmB51chdhkjHF7kb3A";

        var settings = new SBPayWithdrawSettings(
            apiToken,
            notifyToken,
            formId,
            "ACB",
            "王小明",
            "6212262000000000001");
        var withdrawResponse = new SBPayWithdrawResponse(formId);

        var applyInfo = new SBPayWithdrawApplyInfo(settings, withdrawResponse);

        _ = fakePaymentServiceClient.GetWithdrawFormAsync(
                Arg.Is(formId),
                Arg.Any<CancellationToken>())
            .Returns(new WithdrawFormInfo
            {
                Id = formId,
                BrandId = brandId,
                Status = PaymentFlowState.Approved,
                ApplyInfo = JsonSerializer.Serialize(applyInfo, SBPayUtility.SerializerOptions)
            });

        var request = """
            {
                "trade_no": "fefa6779-0a9f-4b8b-a1ec-41f03a4816c3",
                "amount": 500,
                "out_trade_no": "2ddf9804-84f5-11ee-816e-0242ac120003",
                "state": "failed",
                "sign": "082e18fea24e2865c1540b12730e0776"
            }
            """;

        var content = new StringContent(request, Encoding.UTF8, new MediaTypeHeaderValue(MediaTypeNames.Application.Json));

        var response = await httpClient.PostAsync("/withdraw/callback", content);

        var actual = await response.Content.ReadAsStringAsync();

        Assert.Equal("ok", actual);
        _ = fakePaymentServiceClient.Received(1)
            .SubmitWithdrawFormStatusAsync(
                Arg.Is(formId),
                paymentReturn: Arg.Any<string>(),
                cancellationToken: Arg.Any<CancellationToken>());

        _ = fakePaymentClient.Received(1)
            .RejectWithdrawAsync(
                Arg.Is(formId),
                Arg.Is(brandId),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task 代付回調_訂單狀態沖回()
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

        var formId = Guid.Parse("93860bf6-84f6-11ee-816e-0242ac120003");
        var brandId = Guid.NewGuid();
        var apiToken = "recV41A3c0DamceTCn6iU4N74h9isF9c4dtreKHFSWLvKIZhlyAvzh1g4cfp";
        var notifyToken = "Iklv4KrHlyus5tBO6TXkgZaonYE4OIV0WTBtgKkTRdWmB51chdhkjHF7kb3A";

        var settings = new SBPayWithdrawSettings(
            apiToken,
            notifyToken,
            formId,
            "ACB",
            "王小明",
            "6212262000000000001");
        var withdrawResponse = new SBPayWithdrawResponse(formId);

        var applyInfo = new SBPayWithdrawApplyInfo(settings, withdrawResponse);

        _ = fakePaymentServiceClient.GetWithdrawFormAsync(
                Arg.Is(formId),
                Arg.Any<CancellationToken>())
            .Returns(new WithdrawFormInfo
            {
                Id = formId,
                BrandId = brandId,
                Status = PaymentFlowState.Approved,
                ApplyInfo = JsonSerializer.Serialize(applyInfo, SBPayUtility.SerializerOptions)
            });

        var request = """
            {
                "trade_no": "4e29e3bd-1c30-494c-8946-72f1fb3ad06f",
                "amount": 500,
                "out_trade_no": "93860bf6-84f6-11ee-816e-0242ac120003",
                "state": "refund",
                "sign": "3685bcaa1f414039077c67ebe25a41ff"
            }
            """;

        var content = new StringContent(request, Encoding.UTF8, new MediaTypeHeaderValue(MediaTypeNames.Application.Json));

        var response = await httpClient.PostAsync("/withdraw/callback", content);

        var actual = await response.Content.ReadAsStringAsync();

        Assert.Equal("ok", actual);
        _ = fakePaymentServiceClient.Received(1)
            .SubmitWithdrawFormStatusAsync(
                Arg.Is(formId),
                paymentReturn: Arg.Any<string>(),
                cancellationToken: Arg.Any<CancellationToken>());

        _ = fakePaymentClient.Received(1)
            .RejectWithdrawAsync(
                Arg.Is(formId),
                Arg.Is(brandId),
                Arg.Any<CancellationToken>());
    }
}
