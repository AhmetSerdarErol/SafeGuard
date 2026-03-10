using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using FirebaseAdmin.Messaging;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SafeGuard.Data; // AppDbContext için gerekli

namespace SafeGuard.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SosController : ControllerBase
    {
        private readonly AppDbContext _context;

        // Veritabanı motorunu buraya bağlıyoruz
        public SosController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("send")]
        [Authorize]
        public async Task<IActionResult> SendSos([FromBody] LocationData location)
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "Bir Yakınınız";
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdString, out int userId))
                return BadRequest("Kullanıcı kimliği okunamadı.");

            // KONSOLA YAZDIRMA (Eski güzel kodun)
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n============================================");
            Console.WriteLine($"!!! ACİL DURUM !!!");
            Console.WriteLine($"KULLANICI: {username}");
            Console.WriteLine($"KONUM: {location.Latitude}, {location.Longitude}");
            Console.WriteLine($"ZAMAN: {DateTime.Now}");
            Console.WriteLine("============================================\n");
            Console.ResetColor();

            try
            {
                // 1. ADIM: SADECE Onaylanmış (Accepted) yardımcıları bul ve KESİN OLARAK onların User verilerini çek!
                var helperUsers = await _context.Helpers
                    .Where(h => h.UserId == userId && h.Status == "Accepted") // Sadece onaylı dostlar
                    .Join(_context.Users,
                          h => h.HelperId,  // Yardım edecek kişinin ID'si
                          u => u.Id,        // User tablosundaki ID
                          (h, u) => u)      // Direkt karşı tarafın (Savior) User nesnesini al
                    .ToListAsync();

                int atilanFuzeSayisi = 0;

                // 2. ADIM: Her bir yardımcıya zırh delici füzeyi yolla!
                foreach (var hUser in helperUsers)
                {
                    var hedefToken = hUser.FcmToken; // Artık kesinlikle karşı tarafın (arkadaşının) token'ı!

                    if (!string.IsNullOrEmpty(hedefToken))
                    {
                        var message = new Message()
                        {
                            Token = hedefToken,
                            Notification = new Notification
                            {
                                Title = "🚨 ACİL DURUM YARDIM ÇAĞRISI!",
                                Body = $"{username} senden acil yardım istiyor!"
                            },
                            Android = new AndroidConfig
                            {
                                Priority = Priority.High, // KİLİTLİ EKRANI PARÇALAYAN KOMUT
                                Notification = new AndroidNotification
                                {
                                    Sound = "default",
                                    ChannelId = "acil_kanal"
                                }
                            },
                            
                            Data = new Dictionary<string, string>()
                    {
                        { "action", "Emergency" },
                        { "userId", userId.ToString() }, // Telefonun aradığı ID
                        { "userName", username },        // Telefonun aradığı İsim
                        // Virgül/Nokta çakışmasını engellemek için InvariantCulture ekledik:
                        { "latitude", location.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture) },
                        { "longitude", location.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture) }
                    }
                        };

                        // Füzeyi Ateşle!
                        string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                        Console.WriteLine($"🚨 Füze Başarıyla Ulaştı! Hedef: {hUser.FullName} ({hUser.Email})");
                        atilanFuzeSayisi++;
                    }
                }

                return Ok(new { message = $"{atilanFuzeSayisi} kişiye acil durum bildirimi gönderildi!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Füze ateşlenirken kaza oldu: " + ex.Message);
                return StatusCode(500, "Bildirim gönderilemedi.");
            }
        }
    }

    public class LocationData
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}