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
}
