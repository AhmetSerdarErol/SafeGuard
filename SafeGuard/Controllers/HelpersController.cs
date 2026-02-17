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

        // 1. TELEFON NUMARASI İLE İSTEK GÖNDER
        [HttpPost("add")]
        public async Task<IActionResult> AddHelper(HelperDto request)
        {
            // İsteği atanı bul
            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null) return NotFound("Kullanıcı bulunamadı.");

            // Hedef kişiyi TELEFON NUMARASINDAN bul
            var helperUser = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == request.HelperPhoneNumber);
            if (helperUser == null) return NotFound("Bu numaraya ait kullanıcı bulunamadı.");

            // Kendini ekleyemezsin
            if (user.Id == helperUser.Id) return BadRequest("Kendinizi ekleyemezsiniz.");

            // Zaten ekli mi kontrol et
            var existing = await _context.Helpers
                .FirstOrDefaultAsync(h => h.UserId == user.Id && h.HelperId == helperUser.Id);

            if (existing != null) return BadRequest("Bu kişi zaten listenizde veya istek gönderilmiş.");

            // İsteği "BEKLEMEDE" (Pending) olarak kaydet
            var newHelper = new Helper
            {
                UserId = user.Id,
                HelperId = helperUser.Id,
                IsVerified = false,      // HENÜZ ONAYLANMADI
                Status = "Pending",      // DURUM: BEKLEMEDE
                CreatedAt = DateTime.Now
            };

            _context.Helpers.Add(newHelper);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Arkadaşlık isteği gönderildi." });
        }

        // 2. BANA GELEN İSTEKLERİ LİSTELE
        [HttpGet("requests/{myUserId}")]
        public async Task<IActionResult> GetPendingRequests(int myUserId)
        {
            var requests = await _context.Helpers
                .Include(h => h.User) // İsteği atanın ismini alabilmek için
                .Where(h => h.HelperId == myUserId && h.Status == "Pending")
                .Select(h => new
                {
                    RequestId = h.Id,
                    RequesterName = h.User.FullName,
                    RequesterPhone = h.User.PhoneNumber,
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
                // KABUL EDİLDİ: Yeşil yap
                record.IsVerified = true;
                record.Status = "Accepted";
                await _context.SaveChangesAsync();
                return Ok(new { message = "İstek kabul edildi." });
            }
            else
            {
                // REDDEDİLDİ: Kaydı sil
                _context.Helpers.Remove(record);
                await _context.SaveChangesAsync();
                return Ok(new { message = "İstek reddedildi ve silindi." });
            }
        }
        //KABUL EDİLMİŞ ARKADAŞLARI GETİR 
        [HttpGet("contacts/{userId}")]
        public async Task<IActionResult> GetContacts(int userId)
        {
            var contacts = await _context.Helpers
                .Include(h => h.User)
                .Include(h => h.HelperUser) // ARTIK HelperUser DİYORUZ
                .Where(h => (h.UserId == userId || h.HelperId == userId) && h.Status == "Accepted")
                .Select(h => new
                {
                    Name = h.UserId == userId ? h.HelperUser.FullName : h.User.FullName,
                    PhoneNumber = h.UserId == userId ? h.HelperUser.PhoneNumber : h.User.PhoneNumber
                })
                .ToListAsync();

            return Ok(contacts);
        }
    } 
}