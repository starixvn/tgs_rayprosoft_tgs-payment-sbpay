using System.Text.Json.Serialization;

namespace Tgs.Payment.SBPay.ViewModels;

public class CallbackDepositViewModel
{
    /// <summary>
    /// 平台订单号
    /// </summary>
    [JsonPropertyName("trade_no")]
    public string TradeNo { get; set; } = default!;

    /// <summary>
    /// 支付请求金额
    /// </summary>
    [JsonPropertyName("request_amount")]
    public string RequestAmount { get; set; } = default!;

    /// <summary>
    /// 支付实际金额
    /// </summary>
    [JsonPropertyName("amount")]
    public string Amount { get; set; } = default!;

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

    [JsonPropertyName("callback_url")]
    public string CallbackUrl { get; set; } = default!;
}
