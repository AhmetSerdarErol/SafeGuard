using Microsoft.EntityFrameworkCore;
using SafeGuard.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Veritabaný Baðlantýsýný Yapýlandýr
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Controller ve Swagger Servislerini Ekle
builder.Services.AddControllers(); // Sadece bir kere yazýlmalý!
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 3. HTTP Ýstek Hattý (Pipeline) Ayarlarý
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();