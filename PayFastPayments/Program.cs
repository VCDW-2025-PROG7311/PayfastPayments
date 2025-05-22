var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<TransactionService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<PayFastService>(serviceProvider =>
{
    var configuration = builder.Configuration;
    var merchantId = configuration["PayFast__MerchantId"];
    var merchantKey = configuration["PayFast__MerchantKey"];
    var passphrase = configuration["PayFast__Passphrase"];
    var sandboxUrl = configuration["PayFast__SandboxUrl"];    
    return new PayFastService(merchantId, merchantKey, passphrase, sandboxUrl);
});

// Add controllers to handle API routes
builder.Services.AddControllers();

var app = builder.Build();

app.MapOpenApi();
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
