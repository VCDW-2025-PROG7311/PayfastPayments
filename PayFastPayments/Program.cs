using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
    throw new InvalidOperationException("Connection string  not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString,
    sqlOptions =>
    {
        sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        sqlOptions.EnableRetryOnFailure();
        sqlOptions.CommandTimeout(60);
    })
, ServiceLifetime.Scoped);

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

// Configure the HTTP request pipeline (Swagger in development)
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
