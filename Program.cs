using YolBUl;
using Swashbuckle.AspNetCore.SwaggerUI;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

//FindWay find = new FindWay();
//find.FindRoutes(40.78259, 29.94628, 40.7716, 29.9601, "Ogrenci", "Kent Kart");

app.MapGet("/Bus", () =>
{

    FindWay find = new FindWay();
    find.FindRoutes(40.78259, 29.94628, 40.7716, 29.9601, "Ogrenci", "Kent Kart");// dönen değer List<PathResult2> listesi 
    /*
    listenin içindeki nesneler Vehiclede tanımlı PathResult2 classı içinde
    */


    var forecast =  "";
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

record Bus(string value)
{
    public string Value;
}
