var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.AddScoped<Aprimo.CheckFramework.Demo.Services.IAprimoApiService, Aprimo.CheckFramework.Demo.Services.AprimoApiService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

var port = builder.Configuration["Port"] ?? Environment.GetEnvironmentVariable("PORT") ?? "5000";
var url = $"http://localhost:{port}";

app.Run(url);

