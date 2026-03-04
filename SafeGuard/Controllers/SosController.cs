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
                // 1. ADIM: Bu kullanıcının 'Yardımcılarını' veritabanından bul!
                var helpers = await _context.Helpers
                    .Include(h => h.HelperUser)
                    .Where(h => h.UserId == userId)
                    .ToListAsync();

                int atilanFuzeSayisi = 0;

                // 2. ADIM: Her bir yardımcıya zırh delici füzeyi yolla!
                foreach (var helper in helpers)
                {
                    // DİKKAT: Modelindeki isim "Token" ise burayı helper.HelperUser.Token yap
                    var hedefToken = helper.HelperUser?.FcmToken;

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
                                { "senderName", username },
                                { "latitude", location.Latitude.ToString() },
                                { "longitude", location.Longitude.ToString() }
                            }
                        };

                        // Füzeyi Ateşle!
                        string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                        Console.WriteLine($"🚨 Füze Başarıyla Ulaştı! Hedef: {helper.HelperUser.Email}");
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