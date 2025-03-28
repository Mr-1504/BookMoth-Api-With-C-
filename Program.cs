using BookMoth_Api_With_C_.Middleware;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using BookMoth_Api_With_C_.Models;
using BookMoth_Api_With_C_.Services;
using Microsoft.Extensions.FileProviders;
using BookMoth_Api_With_C_.ZaloPay;
using Serilog;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddSingleton<JwtService>();

builder.Services.AddDbContext<BookMothContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("BookMothContext")));

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMemoryCache();

builder.Services.AddScoped<ZaloPayHelper>();
builder.Services.AddTransient<EmailService>();
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddHttpClient(); // Đăng ký IHttpClientFactory
builder.Services.AddSingleton<ZaloPayService>();
builder.Services.AddHostedService<TransactionBackgroundService>();
builder.Services.AddSingleton<FcmService>();

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 50 * 1024 * 1024; // 50MB
});


Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

var app = builder.Build();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "Resources/Images")),
    RequestPath = "/images"
});

var ngrokUrl = builder.Configuration["ZaloPay:NgrokTunnels"];
var ngrokPath = builder.Configuration["ZaloPay:NgrokPath"];

// Khởi động NgrokHelper khi ứng dụng bắt đầu
app.Lifetime.ApplicationStarted.Register(async () =>
{
    NgrokHelper.StartNgrok(ngrokPath); 
    await Task.Delay(5000); // Chờ Ngrok khởi động
    var publicUrl = await NgrokHelper.GetPublicUrl(ngrokUrl);
    Console.WriteLine($"Ngrok Public URL: {publicUrl}");
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseMiddleware<ApiKeyMiddleware>();
app.UseMiddleware<DdosDetectionMiddleware>();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
