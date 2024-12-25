﻿using DepremVeriProjesi.Services;

var builder = WebApplication.CreateBuilder(args);

// Controller desteğini ekleyin
builder.Services.AddControllers();

// MongoDB servislerini ekle
builder.Services.Configure<MongoDBSettings>(builder.Configuration.GetSection("MongoDB"));
builder.Services.AddSingleton<IMongoDBService, MongoDBService>();

// CORS ayarlarını güncelle - tüm kaynakları kabul et
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// CORS politikasını uygula
app.UseCors("AllowAll");

// HTTPS yönlendirmesini ekle
app.UseHttpsRedirection();

// API Controller'ı devreye al
app.MapControllers();

// Test endpoint'i
app.MapGet("/", () => "Deprem Projesi API çalışıyor!");

app.Run();
