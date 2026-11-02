using Enterprise_Api_Sample.Global;
using Enterprise_Api_Sample.Weather;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddExceptionHandler<ApplicationExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseExceptionHandler();

app.MapControllers();
app.MapWeatherSimulation();

app.MapGet("/api/trouble", () =>
{
    throw new Exception("Simulated exception");
})
.WithName("CauseTrouble");

app.Run();
