using Microsoft.EntityFrameworkCore;
using SafeGuard.Data;

var builder = WebApplication.CreateBuilder(args);

// --- SERVÝSLER ---
builder.Services.AddControllers();

// Swagger (Mavi Ekran) Servisleri
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Veritabaný Baðlantýsý
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// --- AYARLAR ---
// Geliþtirme modundaysak Swagger'ý aç
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); // Arayüzü aktif et
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();