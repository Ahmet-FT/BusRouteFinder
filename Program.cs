using YolBUl;
using Swashbuckle.AspNetCore.SwaggerUI;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()   // Tüm origin'lere izin ver (http://localhost:5001 dahil)
            .AllowAnyMethod()   // GET, POST, PUT, DELETE vb. tüm metodlara izin ver
            .AllowAnyHeader();  // Tüm header'lara izin ver
    });
});
// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseCors("AllowAll");
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}






app.MapPost("/calculate", (RouteRequest data) =>
{   
    Console.WriteLine(data.start_lat);
    FindWay find = new FindWay();
    
        
    
        
        
        List<PathResult2> response = find.FindRoutes(data.start_lat,data.start_lon, data.end_lat, data.end_lon, data.kullanici_tipi, "Kent Kart");// dönen değer List<PathResult2> listesi 
        
    var result = response.Select(r => new
    {
        
        
        Path = r.Path.Select(p => new 
        { 
            p.Lat, 
            p.Lon,
            p.Name,
            p.Id,
        }).ToList(),
        
        
    });
    
        return Results.Json(new{result,});
        
    
    
})
.WithName("BusR")
.WithOpenApi();

app.MapPost("/bus", (RouteRequest data) =>
{
    
    FindWay find = new FindWay();
    
        List<PathResult2> response = find.FindRoutes(data.start_lat,data.start_lon, data.end_lat, data.end_lon, data.kullanici_tipi, "Kent Kart");// dönen değer List<PathResult2> listesi 
        
    var result = response.Select(r => new
    {   
        
        Path = r.Path.Select(p => new 
        { 
            p.Lat, 
            p.Lon,
            p.Name,
            p.Id,
        }).ToList(),
        
        
    });
        return  new{result };
    
    
})
.WithName("BusRoute")
.WithOpenApi();


app.Run();
public record RouteRequest(
    double start_lat  ,
    double start_lon,
    double end_lat,
    double end_lon,
    string kullanici_tipi
    // string KartTuru = "Kent Kart"
);