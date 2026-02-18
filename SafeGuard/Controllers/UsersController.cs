using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeGuard.Data;
using SafeGuard.Dtos;
using SafeGuard.Models;

namespace SafeGuard.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("register")]
        public async Task<ActionResult<User>> Register(UserDto request)
        {
            // 1. E-posta zaten var mı kontrol et
            if (_context.Users.Any(u => u.Email == request.Email))
                return BadRequest("Bu e-posta adresi zaten kullanılıyor.");

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // 2. Yeni kullanıcıyı oluştur
            var user = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                Username = request.Email, // KRİTİK DÜZELTME: request.Username yerine Email kullandık
                PasswordHash = passwordHash,
                Role = "User",
                PhoneNumber = request.PhoneNumber, // Dto'da = "" dediğimiz için null gelmez

                BloodType = request.BloodType,
                ChronicDiseases = request.ChronicDiseases,
                Allergies = request.Allergies,
                Surgeries = request.Surgeries,
                Habits = request.Habits,

                Smoker = false,
                AlcoholConsumption = false
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return Ok(user);
        }

        [HttpPost("login")]
        public async Task<ActionResult<User>> Login(UserLoginDto request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null) return BadRequest("Kullanıcı bulunamadı.");

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return BadRequest("Yanlış şifre.");

            return Ok(user);
        }
        // 1. FOTOĞRAF YÜKLEME
        [HttpPost("upload-photo/{userId}")]
        public async Task<IActionResult> UploadPhoto(int userId, IFormFile file)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("Kullanıcı yok");
            if (file == null || file.Length == 0) return BadRequest("Dosya yok");

            // Dosya ismini hazırla
            var fileName = $"User_{userId}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

            // Kaydet
            using (var stream = new FileStream(Path.Combine(uploadPath, fileName), FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // URL'i güncelle
            user.ProfilePhotoUrl = $"uploads/{fileName}";
            await _context.SaveChangesAsync();

            return Ok(new { Path = user.ProfilePhotoUrl });
        }

        // 2. BİLGİ GÜNCELLEME (Kan Grubu Dahil)
        [HttpPut("update-info/{id}")]
        public async Task<IActionResult> UpdateUserInfo(int id, [FromBody] User updatedData)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.FullName = updatedData.FullName;
            user.PhoneNumber = updatedData.PhoneNumber;
            user.BloodType = updatedData.BloodType;

            await _context.SaveChangesAsync();
            return Ok(user);
        }
    }
}