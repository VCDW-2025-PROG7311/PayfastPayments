using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine(builder.Configuration.GetConnectionString("DefaultConnection"));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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
