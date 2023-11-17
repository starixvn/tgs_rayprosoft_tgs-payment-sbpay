using System.Text.Json.Serialization;

namespace Tgs.Payment.SBPay.ViewModels;

public class CallbackWithdrawViewModel
{
    /// <summary>
    /// 平台订单号
    /// </summary>
    [JsonPropertyName("trade_no")]
    public string TradeNo { get; set; } = default!;

    /// <summary>
    /// 支付请求金额
    /// </summary>
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    /// <summary>
    /// 商户订单号
    /// </summary>
    [JsonPropertyName("out_trade_no")]
    public string OutTradeNo { get; set; } = default!;

    /// <summary>
    /// 状态码
    /// </summary>
    [JsonPropertyName("state")]
    public string State { get; set; } = default!;

    [JsonPropertyName("sign")]
    public string Sign { get; set; } = default!;    

    /// <summary>
    /// 失败原因（须申请开启，仅失败订单会有）
    /// </summary>
    [JsonPropertyName("errors")]
    public string? Errors { get; set; }
}
