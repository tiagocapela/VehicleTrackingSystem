// ================================================================================================
// src/VehicleTracking.Web/Program.cs
// ================================================================================================

using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using VehicleTracking.Web.Models;
using VehicleTracking.Web.Services;
using VehicleTracking.Web.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = false;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddDbContext<VehicleTrackingContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient<TcpMicroserviceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["TcpMicroservice:BaseUrl"] ?? "https://localhost:5001");
});

builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.PayloadSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    });

var app = builder.Build();

// Configure pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<VehicleTrackingHub>("/vehicleTrackingHub");

app.Run();



//// Program.cs - Main MVC Application
//using Microsoft.EntityFrameworkCore;
//using System.Text.Json;
//using System.Text.Json.Serialization;
//using VehicleTracking.Models;
//using VehicleTracking.Services;
//using VehicleTrackingSystem.Hubs;

//var builder = WebApplication.CreateBuilder(args);

//// Add services
//builder.Services.AddControllersWithViews()
//    //.AddJsonOptions(x =>
//    //        x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve); ;
//    .AddJsonOptions(options =>
//     {
//         options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
//         options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
//         options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
//     });

//builder.Services.AddDbContext<VehicleTrackingContext>(options =>
//    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

//builder.Services.AddHttpClient<TcpMicroserviceClient>(client =>
//{
//    client.BaseAddress = new Uri(builder.Configuration["TcpMicroservice:BaseUrl"]);
//});

//builder.Services.AddScoped<IVehicleService, VehicleService>();
//builder.Services.AddSignalR();

//var app = builder.Build();

//// Configure pipeline
//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Home/Error");
//    app.UseHsts();
//}

//app.UseHttpsRedirection();
//app.UseStaticFiles();
//app.UseRouting();
//app.UseAuthorization();

//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Home}/{action=Index}/{id?}");

//app.MapHub<VehicleTrackingHub>("/vehicleTrackingHub");

//app.Run();



