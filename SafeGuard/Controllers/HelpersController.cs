using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeGuard.Data;
using SafeGuard.Dtos;
using SafeGuard.Models;

namespace SafeGuard.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HelpersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public HelpersController(AppDbContext context)
        {
            _context = context;
        }

        // 1. YARDIMCI EKLEME İSTEĞİ GÖNDER (Pending Modunda)
        [HttpPost("add")]
        public async Task<IActionResult> AddHelper(HelperDto request)
        {
            // Kullanıcıyı bul (İsteği atan)
            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null) return NotFound("Kullanıcı bulunamadı.");

            // Yardımcı olacak kişiyi bul (İstek atılan)
            var helperUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.HelperEmail);
            if (helperUser == null) return NotFound("Bu e-posta adresine sahip bir kullanıcı bulunamadı.");

            // Kendini ekleyemez
            if (user.Id == helperUser.Id) return BadRequest("Kendinizi yardımcı olarak ekleyemezsiniz.");

            // Zaten ekli mi?
            var existing = await _context.Helpers
                .FirstOrDefaultAsync(h => h.UserId == user.Id && h.HelperId == helperUser.Id);

            if (existing != null) return BadRequest("Bu kişi zaten listenizde veya istek gönderilmiş.");

            var newHelper = new Helper
            {
                UserId = user.Id,
                HelperId = helperUser.Id,
                IsVerified = false, // ARTIK DİREKT ONAYLAMIYORUZ!
                Status = "Pending", // Beklemede
                CreatedAt = DateTime.Now
            };

            _context.Helpers.Add(newHelper);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Arkadaşlık isteği gönderildi." });
        }

        // 2. BANA GELEN İSTEKLERİ LİSTELE
        // (Biri beni yardımcı olarak eklemek istiyorsa burada görürüm)
        [HttpGet("requests/{myUserId}")]
        public async Task<IActionResult> GetPendingRequests(int myUserId)
        {
            // HelperId'si BEN olan ama henüz onaylanmamış (IsVerified=false) kayıtları getir
            var requests = await _context.Helpers
                .Include(h => h.User) // İsteği atan kişinin adını görmek için
                .Where(h => h.HelperId == myUserId && h.IsVerified == false)
                .Select(h => new
                {
                    RequestId = h.Id,
                    RequesterName = h.User.FullName,
                    RequesterEmail = h.User.Email,
                    RequestDate = h.CreatedAt
                })
                .ToListAsync();

            return Ok(requests);
        }

        // 3. İSTEĞİ CEVAPLA (Kabul Et / Reddet)
        [HttpPost("respond")]
        public async Task<IActionResult> RespondToRequest([FromBody] RespondDto request)
        {
            var record = await _context.Helpers.FindAsync(request.RequestId);
            if (record == null) return NotFound("İstek bulunamadı.");

            if (request.Accept)
            {
                // Kabul edildiyse
                record.IsVerified = true;
                record.Status = "Accepted";
                await _context.SaveChangesAsync();
                return Ok(new { message = "İstek kabul edildi. Artık güvendesiniz." });
            }
            else
            {
                // Reddedildiyse kaydı siliyoruz
                _context.Helpers.Remove(record);
                await _context.SaveChangesAsync();
                return Ok(new { message = "İstek reddedildi ve silindi." });
            }
        }
    }

    // Küçük bir DTO sınıfı (Dosyanın en altına ekleyebilirsin veya Dtos klasörüne)
    public class RespondDto
    {
        public int RequestId { get; set; }
        public bool Accept { get; set; }
    }
}