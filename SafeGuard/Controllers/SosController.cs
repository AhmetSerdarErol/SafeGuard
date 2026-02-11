using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SafeGuard.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SosController : ControllerBase
    {
        // Sadece giriş yapmış (Token'ı olan) kullanıcılar SOS gönderebilir
        [HttpPost("send")]
        [Authorize]
        public IActionResult SendSos([FromBody] LocationData location)
        {
            // Token'dan kimin yardım istediğini bulalım
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // KONSOLA KIRMIZI YAZALIM Kİ BELLİ OLSUN
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n============================================");
            Console.WriteLine($"!!! ACİL DURUM !!!");
            Console.WriteLine($"KULLANICI: {username}");
            Console.WriteLine($"KONUM: {location.Latitude}, {location.Longitude}");
            Console.WriteLine($"ZAMAN: {DateTime.Now}");
            Console.WriteLine("============================================\n");
            Console.ResetColor();

            return Ok(new { message = "Yardım çağrısı alındı, ekipler yönlendiriliyor!" });
        }
    }

    public class LocationData
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}