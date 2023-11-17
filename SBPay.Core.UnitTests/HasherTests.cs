using Microsoft.Extensions.Logging.Abstractions;
using Tgs.Payment.SBPay;

namespace SBPay.Core.UnitTests;

public class HasherTests
{
    [Fact]
    public void Withdraw_Sample()
    {
        var apiToken = "recV41A3c0DamceTCn6iU4N74h9isF9c4dtreKHFSWLvKIZhlyAvzh1g4cfp";
        var notifyToken = "Iklv4KrHlyus5tBO6TXkgZaonYE4OIV0WTBtgKkTRdWmB51chdhkjHF7kb3A";
        var dict = new Dictionary<string, string?>
        {
            ["account_number"] = "6212262000000000001",
            ["amount"] = "500",
            ["bank_id"] = "ACB",
            ["bank_owner"] = "DAO VAN THANG",
            ["callback_url"] = "https://www.example.com/notify",
            ["out_trade_no"] = "83d91476-89f5-4540-8f6a-f645ad567bbc",
            ["verify_channel_no"] = "1"
        };
        var expected = "58185ad4cc48209a87e57ce7daa3156a";

        var sut = new SBPayHasher(NullLogger<SBPayHasher>.Instance);

        var actual = sut.ComputeHash(apiToken, notifyToken, dict);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Deposit_Callback_Sample()
    {
        var apiToken = "recV41A3c0DamceTCn6iU4N74h9isF9c4dtreKHFSWLvKIZhlyAvzh1g4cfp";
        var notifyToken = "Iklv4KrHlyus5tBO6TXkgZaonYE4OIV0WTBtgKkTRdWmB51chdhkjHF7kb3A";

        var dict = new Dictionary<string, string?>
        {
            ["trade_no"] = "fdd49c43-e1c1-49da-9d23-8027f5412fe6",
            ["amount"] = "300000",
            ["request_amount"] = "300000",
            ["out_trade_no"] = "lt0085DB4AE5444B_60509043_112159",
            ["state"] = "completed",
            ["callback_url"] = "https://www.example.com/notify",
            ["sign"] = "7633bcfde2ecb3a9ea65ae234016eb20"
        };

        _ = dict.Remove("request_amount");
        _ = dict.Remove("callback_url");

        var expected = "7633bcfde2ecb3a9ea65ae234016eb20";

        var sut = new SBPayHasher(NullLogger<SBPayHasher>.Instance);

        var actual = sut.ComputeHash(apiToken, notifyToken, dict);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void 代收回調()
    {
        var apiToken = "X9VmKmXXWn3xviG1XojhW2LYzn72e0B3ockBEsYSdXrjzmaoufpg4fVUnQy4";
        var notifyToken = "eqfB75dajWSyWYQ6x8D0Q1oKKsUkTPnA208VZtyQVBIJX1wqUIArA3MOWU5y";
        var dict = new Dictionary<string, string?>
        {
            ["trade_no"] = "5e22aef2-33e1-405a-8a3c-44a734331ccf",
            ["request_amount"] = 200000.ToString(),
            ["amount"] = 200000.ToString(),
            ["out_trade_no"] = "a2fb10ed-9f78-46d5-b927-ec2a36816761",
            ["state"] = "completed",
            ["sign"] = "d47e00e2b8549f04bdf6bb328c41ad91",
        };
        var expected = "d47e00e2b8549f04bdf6bb328c41ad91";

        var sut = new SBPayHasher(NullLogger<SBPayHasher>.Instance);
        _ = dict.Remove("request_amount");

        var actual = sut.ComputeHash(apiToken, notifyToken, dict);

        Assert.Equal(expected, actual);        
    }

    [Fact]
    public void 代付回調_成功()
    {
        var apiToken = "recV41A3c0DamceTCn6iU4N74h9isF9c4dtreKHFSWLvKIZhlyAvzh1g4cfp";
        var notifyToken = "Iklv4KrHlyus5tBO6TXkgZaonYE4OIV0WTBtgKkTRdWmB51chdhkjHF7kb3A";
 
        var dict = new Dictionary<string, string?>
        {
            ["trade_no"] = "1d786b53-b17d-4aa3-8ad0-bc9a57e100fe",
            ["amount"] = 500.ToString(),            
            ["out_trade_no"] = "4d9a37f0-84f3-11ee-816e-0242ac120003",
            ["state"] = "completed",
            ["sign"] = "76a55d5636b9285cdc10cf5cb33811d1",
        };
        var expected = "76a55d5636b9285cdc10cf5cb33811d1";

        var sut = new SBPayHasher(NullLogger<SBPayHasher>.Instance);        

        var actual = sut.ComputeHash(apiToken, notifyToken, dict);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void 代付回調_失敗()
    {
        var apiToken = "recV41A3c0DamceTCn6iU4N74h9isF9c4dtreKHFSWLvKIZhlyAvzh1g4cfp";
        var notifyToken = "Iklv4KrHlyus5tBO6TXkgZaonYE4OIV0WTBtgKkTRdWmB51chdhkjHF7kb3A";
       
        var dict = new Dictionary<string, string?>
        {
            ["trade_no"] = "fefa6779-0a9f-4b8b-a1ec-41f03a4816c3",
            ["amount"] = 500.ToString(),
            ["out_trade_no"] = "2ddf9804-84f5-11ee-816e-0242ac120003",
            ["state"] = "failed",
            ["sign"] = "082e18fea24e2865c1540b12730e0776",
        };
        var expected = "082e18fea24e2865c1540b12730e0776";

        var sut = new SBPayHasher(NullLogger<SBPayHasher>.Instance);

        var actual = sut.ComputeHash(apiToken, notifyToken, dict);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void 代付回調_沖回()
    {
        var apiToken = "recV41A3c0DamceTCn6iU4N74h9isF9c4dtreKHFSWLvKIZhlyAvzh1g4cfp";
        var notifyToken = "Iklv4KrHlyus5tBO6TXkgZaonYE4OIV0WTBtgKkTRdWmB51chdhkjHF7kb3A";

        var dict = new Dictionary<string, string?>
        {
            ["trade_no"] = "4e29e3bd-1c30-494c-8946-72f1fb3ad06f",
            ["amount"] = 500.ToString(),
            ["out_trade_no"] = "93860bf6-84f6-11ee-816e-0242ac120003",
            ["state"] = "refund",
            ["sign"] = "3685bcaa1f414039077c67ebe25a41ff",
        };
        var expected = "3685bcaa1f414039077c67ebe25a41ff";

        var sut = new SBPayHasher(NullLogger<SBPayHasher>.Instance);

        var actual = sut.ComputeHash(apiToken, notifyToken, dict);

        Assert.Equal(expected, actual);
    }
}
