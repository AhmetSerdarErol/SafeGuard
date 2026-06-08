using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using FirebaseAdmin.Messaging;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SafeGuard.Data;

namespace SafeGuard.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class SosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SosController(AppDbContext context)
        {
            _context = context;
        }

        public class SosLocationData
        {
            public int userId { get; set; }
            public double latitude { get; set; }
            public double longitude { get; set; }
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendSos([FromBody] SosLocationData data)
        {
            var dbUser = await _context.Users.FindAsync(data.userId);
            if (dbUser == null) return BadRequest("Kullanıcı bulunamadı.");

            dbUser.IsSosActive = true;
            dbUser.LastStatusUpdate = DateTime.Now;

            dbUser.Latitude = data.latitude;
            dbUser.Longitude = data.longitude;

            await _context.SaveChangesAsync();

            var username = dbUser.FullName;
            int currentUserId = dbUser.Id;

            var helperUsers = await _context.Helpers
                .Where(h => h.UserId == currentUserId && h.Status == "Accepted")
                .Join(_context.Users, h => h.HelperId, u => u.Id, (h, u) => u)
                .ToListAsync();

            int atilanFuzeSayisi = 0;

            foreach (var hUser in helperUsers)
            {
                var hedefToken = hUser.FcmToken;
                if (!string.IsNullOrEmpty(hedefToken))
                {
                    try
                    {
                        username = !string.IsNullOrWhiteSpace(dbUser.FullName) ? dbUser.FullName : "Bilinmeyen Kullanıcı";

                        var message = new FirebaseAdmin.Messaging.Message()
                        {
                            Token = hedefToken,
                            Android = new FirebaseAdmin.Messaging.AndroidConfig
                            {
                                Priority = FirebaseAdmin.Messaging.Priority.High
                            },
                            Data = new Dictionary<string, string>()
                    {
                        { "action", "Emergency" },
                        { "senderId", currentUserId.ToString() },
                        { "senderName", username },
                        { "latitude", data.latitude.ToString(System.Globalization.CultureInfo.InvariantCulture) },
                        { "longitude", data.longitude.ToString(System.Globalization.CultureInfo.InvariantCulture) }
                    }
                        };

                        await FirebaseAdmin.Messaging.FirebaseMessaging.DefaultInstance.SendAsync(message);
                        Console.WriteLine($"🚨 Füze Başarıyla Ulaştı! Hedef: {hUser.FullName}");
                        atilanFuzeSayisi++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ {hUser.FullName} kişisine füze atılamadı. Kaza: " + ex.Message);
                        continue;
                    }
                }
            }

            return Ok(new { message = $"{atilanFuzeSayisi} kişiye bildirim gönderildi!" });
        }
       
        [HttpPost("UploadAudio")]
        public async Task<IActionResult> UploadAudio(IFormFile audioFile, [FromForm] int userId)
        {
            try
            {
                if (audioFile == null || audioFile.Length == 0)
                    return BadRequest("Dosya boş veya gelmedi.");

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "audio");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileName = $"sos_{userId}_{DateTime.Now:yyyyMMddHHmmss}.m4a";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await audioFile.CopyToAsync(stream);
                }

                var fileUrl = $"/uploads/audio/{fileName}";
                Console.WriteLine($"🎤 [KARA KUTU] {userId} ID'li kullanıcının ses kaydı sunucuya ulaştı!");

                return Ok(new { Message = "Kara kutu başarıyla sunucuya ulaştı!", Url = fileUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Sunucu hatası: {ex.Message}");
            }
        }
    }
}