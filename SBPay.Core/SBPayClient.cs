using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace Tgs.Payment.SBPay;

public class SBPayClient
{
    private readonly HttpClient _httpClient;
    private readonly SBPayHasher _hasher;
    private readonly ILogger<SBPayClient> _logger;

    public SBPayClient(
        HttpClient httpClient,
        SBPayHasher hasher,
        ILogger<SBPayClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<SBPayClientDepositResponse> DepositAsync(string apiToken, DepositOrder order, CancellationToken cancellationToken = default)
    {
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);

        var dict = new Dictionary<string, string?>
        {
            ["amount"] = order.Amount.ToString(),
            ["callback_url"] = order.CallbackUrl.AbsoluteUri,
            ["out_trade_no"] = order.FormId.ToString(),
        };

        if (order.Bank is not null)
            dict["bank"] = order.Bank.ToString();

        var response = await _httpClient
            .PostAsJsonAsync("/api/transaction", dict, cancellationToken)
            .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("SBPayClient Deposit Body:{body}", body);

        var node = JsonNode.Parse(body) ?? throw new ApiFailException("Deposit response can't read.");

        if (node["success"]!.GetValue<bool>() != true)
            throw new ApiFailException(string.Format("message:{0}, errors:{1}", node["message"]!.ToString(), node["errors"]!.ToString()));

        var data = node["data"]!;

        return new SBPayClientDepositResponse(
            Amount: data["amount"]!.GetValue<decimal>(),
            FormId: data["out_trade_no"]!.GetValue<Guid>(),
            ThirdPartyOrderNo: data["trade_no"]!.GetValue<string>(),
            RedirectUrl: new Uri(data["uri"]!.GetValue<string>()));
    }

    public async ValueTask<SBPayClientQueryDepositResponse> QueryDepositFormStatusAsync(string apiToken, Guid formId, CancellationToken cancellationToken = default)
    {
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);

        var response = await _httpClient
            .GetAsync($"/api/transaction/{formId}", cancellationToken)
            .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("SBPayClient Query Deposit Form Body:{body}", body);

        var node = JsonNode.Parse(body) ?? throw new ApiFailException("Query Deposit response can't read.");

        if (node["success"]!.GetValue<bool>() != true)
            throw new ApiFailException(node["message"]!.ToString());

        var data = node["data"]!;

        return new SBPayClientQueryDepositResponse(
            State: data["state"]!.GetValue<string>());
    }

    public async ValueTask<SBPayClientWithdrawResponse> WithdrawAsync(string apiToken, string notifyToken, WithdrawOrder order, CancellationToken cancellationToken = default)
    {
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);

        var dict = new Dictionary<string, string?>
        {
            ["out_trade_no"] = order.FormId.ToString(),
            ["amount"] = order.Amount.ToString(),
            ["callback_url"] = order.CallbackUrl.AbsoluteUri,
            ["bank_id"] = order.BankId,
            ["bank_owner"] = order.BankOwner,
            ["account_number"] = order.AccountNumber,
        };

        dict["sign"] = _hasher.ComputeHash(apiToken, notifyToken, dict);

        var response = await _httpClient
            .PostAsJsonAsync("/api/payment", dict, cancellationToken)
            .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("SBPayClient Withdraw Body:{body}", body);

        var node = JsonNode.Parse(body) ?? throw new ApiFailException("Withdraw response can't read.");

        if (node["success"]!.GetValue<bool>() != true)
            throw new ApiFailException(string.Format("message:{0}, errors:{1}", node["message"]!.ToString(), node["errors"]!.ToString()));

        var data = node["data"]!;

        return new SBPayClientWithdrawResponse(
            FormId: data["out_trade_no"]!.GetValue<Guid>(),
            ThirdPartyOrderNo: data["trade_no"]!.GetValue<string>(),
            Amount: data["amount"]!.GetValue<decimal>());
    }

    public async ValueTask<decimal> QueryBalanceAsync(string apiToken, CancellationToken cancellationToken = default)
    {
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);

        var response = await _httpClient
            .GetAsync("/api/balance/inquiry", cancellationToken)
            .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("SBPayClient Query Merchant Balance Body:{body}", body);

        var node = JsonNode.Parse(body) ?? throw new ApiFailException("Query Merchant Balance response can't read.");

        if (node["success"]!.GetValue<bool>() != true)
            throw new ApiFailException("Query Merchant Balance failed");

        var data = node["data"]!;

        return decimal.Parse(data["balance"]!.GetValue<string>());
    }
}
