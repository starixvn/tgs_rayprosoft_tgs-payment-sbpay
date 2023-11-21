using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using RichardSzalay.MockHttp;
using Tgs.Payment.SBPay;

namespace SBPay.Core.UnitTests;

public class SBPayClientTests
{
    [Fact]
    public async Task 代收接口_純卡網關_成功()
    {
        var mockHttp = new MockHttpMessageHandler();
        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://tianciv310528.com");
        var signer = new SBPayHasher(NullLogger<SBPayHasher>.Instance);

        var sut = new SBPayClient(httpClient, signer, NullLogger<SBPayClient>.Instance);

        var formId = Guid.Parse("86365b8e-8368-11ee-8aa3-0242ac120006");
        var apiToken = "X9VmKmXXWn3xviG1XojhW2LYzn72e0B3ockBEsYSdXrjzmaoufpg4fVUnQy4";

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

        var order = new DepositOrder(
            100M,
            formId,
            new Uri("https://dev-payment.rpgp.cc/sbpay/deposit/callback"),
            null);

        var actual = await sut.DepositAsync(apiToken, order);

        Assert.Equal(formId, actual.FormId);
        Assert.Equal("1eaf0f7a-a8d3-4567-a689-fcf0405fbb11", actual.ThirdPartyOrderNo);
    }

    [Fact]
    public async Task 代付接口_成功()
    {
        var mockHttp = new MockHttpMessageHandler();
        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://tianciv310528.com");
        var signer = new SBPayHasher(NullLogger<SBPayHasher>.Instance);

        var sut = new SBPayClient(httpClient, signer, NullLogger<SBPayClient>.Instance);

        var formId = Guid.Parse("48f5cb14-838c-11ee-8aa3-0242ac120006");
        var apiToken = "recV41A3c0DamceTCn6iU4N74h9isF9c4dtreKHFSWLvKIZhlyAvzh1g4cfp";
        var notifyToken = "Iklv4KrHlyus5tBO6TXkgZaonYE4OIV0WTBtgKkTRdWmB51chdhkjHF7kb3A";

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

        var order = new WithdrawOrder(
            formId,
            500m,
            new Uri("https://dev-payment.rpgp.cc/sbpay/withdraw/callback"),
            BankId: "ACB",
            BankOwner: "王小明",
            AccountNumber: "6212262000000000001");

        var actual = await sut.WithdrawAsync(apiToken, notifyToken, order);

        Assert.Equal(formId, actual.FormId);
        Assert.Equal("887b4e0d-2d10-40a4-9266-6930276a0bd9", actual.ThirdPartyOrderNo);
    }

    [Fact]
    public async Task 查詢商戶餘額()
    {
        var mockHttp = new MockHttpMessageHandler();

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://tianciv310528.com");

        var signer = new SBPayHasher(NullLogger<SBPayHasher>.Instance);

        var sut = new SBPayClient(httpClient, signer, NullLogger<SBPayClient>.Instance);

        var apiToken = "recV41A3c0DamceTCn6iU4N74h9isF9c4dtreKHFSWLvKIZhlyAvzh1g4cfp";

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

        var actual = await sut.QueryBalanceAsync(apiToken);

        Assert.Equal(988400m, actual);
    }

    [Fact]
    public async Task 代收查詢_狀態逾時()
    {
        var mockHttp = new MockHttpMessageHandler();

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://tianciv310528.com");

        var signer = new SBPayHasher(NullLogger<SBPayHasher>.Instance);

        var sut = new SBPayClient(httpClient, signer, NullLogger<SBPayClient>.Instance);

        var apiToken = "euIAhKfGK8oAaXIGgfhDSDWflKO003zz2tQl90jb1vuw6vz83CVxkCoptINt";
        var formId = Guid.Parse("0ff96c9d-d3f7-467f-90d2-9917820e89bf");

        _ = mockHttp
            .Expect(HttpMethod.Get, "https://tianciv310528.com/api/transaction/0ff96c9d-d3f7-467f-90d2-9917820e89bf")
            .WithHeaders(new Dictionary<string, string>
            {
                { "Accept", "application/json" },
                { "Authorization", $"Bearer {apiToken}" }
            })
            .Respond(HttpStatusCode.OK, new StringContent("""
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

        var actual = await sut.QueryDepositFormStatusAsync(apiToken, formId);

        Assert.Equal("timeout", actual.State);
    }

    [Fact]
    public async Task 代收查詢_狀態處理中()
    {
        var mockHttp = new MockHttpMessageHandler();

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://tianciv310528.com");

        var signer = new SBPayHasher(NullLogger<SBPayHasher>.Instance);

        var sut = new SBPayClient(httpClient, signer, NullLogger<SBPayClient>.Instance);

        var apiToken = "euIAhKfGK8oAaXIGgfhDSDWflKO003zz2tQl90jb1vuw6vz83CVxkCoptINt";
        var formId = Guid.Parse("d083e4de-cc49-48ea-bcc5-c23d5edef841");

        _ = mockHttp
            .Expect(HttpMethod.Get, "https://tianciv310528.com/api/transaction/d083e4de-cc49-48ea-bcc5-c23d5edef841")
            .WithHeaders(new Dictionary<string, string>
            {
                { "Accept", "application/json" },
                { "Authorization", $"Bearer {apiToken}" }
            })
            .Respond(HttpStatusCode.OK, new StringContent("""
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

        var actual = await sut.QueryDepositFormStatusAsync(apiToken, formId);

        Assert.Equal("processing", actual.State);
    }

    [Fact]
    public void 代收查詢_未找到()
    {
        var mockHttp = new MockHttpMessageHandler();

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://tianciv310528.com");

        var signer = new SBPayHasher(NullLogger<SBPayHasher>.Instance);

        var sut = new SBPayClient(httpClient, signer, NullLogger<SBPayClient>.Instance);

        var apiToken = "recV41A3c0DamceTCn6iU4N74h9isF9c4dtreKHFSWLvKIZhlyAvzh1g4cfp";
        var formId = Guid.Parse("d083e4de-cc49-48ea-bcc5-c23d5edef841");

        _ = mockHttp
            .Expect(HttpMethod.Get, "https://tianciv310528.com/api/transaction/d083e4de-cc49-48ea-bcc5-c23d5edef841")
            .WithHeaders(new Dictionary<string, string>
            {
                { "Accept", "application/json" },
                { "Authorization", $"Bearer {apiToken}" }
            })
            .Respond(HttpStatusCode.OK, new StringContent("""
                {
                    "success": false,
                    "status_code": 404,
                    "message": "Not Found",
                    "errors": []
                }
                """));

        _ = Assert.ThrowsAsync<ApiFailException>(async () => await sut.QueryDepositFormStatusAsync(apiToken, formId));
    }
}
