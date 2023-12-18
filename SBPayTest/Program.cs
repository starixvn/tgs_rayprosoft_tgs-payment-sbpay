using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tgs.Payment.Clients;
using Tgs.Payment.SBPay.Client;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddPaymentClient()    
    .AddPhoenixToolkitsMessageQueue()
    .AddNatsMessageQueue(natsConfig => natsConfig
        .ConfigQueueOptions((opt, _) => opt.Url = "nats://localhost:4222"))
    .AddNatsGlobPatternExchange("*");

var app = builder.Build();

await app.StartAsync().ConfigureAwait(false);

var logger = app.Services.GetRequiredService<ILogger<Program>>();

var paymentClient = app.Services.GetRequiredService<IPaymentClient>();


//代收 純卡網關
//var apiToken = "X9VmKmXXWn3xviG1XojhW2LYzn72e0B3ockBEsYSdXrjzmaoufpg4fVUnQy4";
//var notifyToken = "eqfB75dajWSyWYQ6x8D0Q1oKKsUkTPnA208VZtyQVBIJX1wqUIArA3MOWU5y";

//var depositSettings = new SBPayDepositSettings(apiToken, notifyToken, Guid.NewGuid(), null);

//var depositReturn = await paymentClient.DepositAsync(
//    "tgs.payment.sbpay.deposit",
//    200000M,
//    JsonSerializer.Serialize(depositSettings, SBPayUtility.SerializerOptions)).ConfigureAwait(false);

//logger.LogInformation("DepositReturn: {depositReturn}", depositReturn);


var apiToken = "recV41A3c0DamceTCn6iU4N74h9isF9c4dtreKHFSWLvKIZhlyAvzh1g4cfp";
var notifyToken = "Iklv4KrHlyus5tBO6TXkgZaonYE4OIV0WTBtgKkTRdWmB51chdhkjHF7kb3A";


var withdrawSettings = new SBPayWithdrawSettings(    
    apiToken,
    notifyToken,
    Guid.NewGuid(),
    BankId: "ACB",
    BankOwner: "王小明",
    AccountNumber: "6212262000000000001",
    ExchangeRate: 1m);

var withdrawReturn = await paymentClient.WithdrawAsync(
    "tgs.payment.sbpay.withdraw",
    100M,
    JsonSerializer.Serialize(withdrawSettings, SBPayUtility.SerializerOptions)).ConfigureAwait(false);

logger.LogInformation("{withdrawReturn}", withdrawReturn);