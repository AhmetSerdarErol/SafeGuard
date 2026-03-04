using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeGuard.Backend.DTOs;
using SafeGuard.Data;
using SafeGuard.Dtos;
using SafeGuard.Models;
using FirebaseAdmin.Messaging;

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

        [HttpPost("update-fcm-token")]
        public async Task<IActionResult> UpdateFcmToken([FromBody] UpdateTokenDto request)
        {
            // 1. Veritabanında bu id'ye sahip kullanıcıyı bul
            var user = await _context.Users.FindAsync(request.UserId);

            if (user == null)
                return NotFound("Kullanıcı bulunamadı.");

            // 2. Kullanıcının adresini (Token) yeni gelen adresle değiştir
            user.FcmToken = request.Token;

            // 3. Değişiklikleri veritabanına kaydet
            await _context.SaveChangesAsync();

            return Ok(new { message = "Postacının adresi (Token) başarıyla veritabanına işlendi!" });
        }

        [HttpPost("send-sos")]
        public async Task<IActionResult> SendSosAlert(int senderId, int targetUserId)
        {
            var senderUser = await _context.Users.FindAsync(senderId);
            var targetUser = await _context.Users.FindAsync(targetUserId);

            if (targetUser == null || string.IsNullOrEmpty(targetUser.FcmToken))
                return BadRequest("Hedef kullanıcının cihaz adresi bulunamadı!");

            var message = new Message()
            {
                Token = targetUser.FcmToken,

                Data = new Dictionary<string, string>()
                {
                    { "senderName", senderUser?.FullName ?? "Bir arkadaşın" },
                    { "title", "🚨 ACİL DURUM (SOS) 🚨" },
                    { "message", "Acil yardıma ihtiyacı var!" }
                },
                Android = new AndroidConfig()
                {
                    Priority = Priority.High
                }
            };

            try
            {
                string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                return Ok(new { success = true, info = "Füze hedefi vurdu!", details = response });
            }
            catch (Exception ex)
            {
                return BadRequest($"Füze ateşlenemedi: {ex.Message}");
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDto request)
        {
            // 1. E-posta zaten var mı kontrol et
            if (_context.Users.Any(u => u.Email == request.Email))
                return BadRequest("Bu e-posta adresi zaten kullanılıyor.");

            // Şifreyi güvenlik için kriptoluyoruz
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // 2. Yeni kullanıcıyı oluştur
            var newUser = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                Password = passwordHash,
                PhoneNumber = request.PhoneNumber,
                BloodType = request.BloodType,
                Height = request.Height,
                Weight = request.Weight,
                Allergies = request.Allergies,
                MedicalConditions = request.MedicalConditions,
                Medications = request.Medications,
                OrganStatus = request.OrganStatus,
                OrganDetails = request.OrganDetails,
                AlcoholUse = request.AlcoholUse,
                SmokingHabit = request.SmokingHabit
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Kayıt başarılı!" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto request)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == request.Email);

            // 1. Kullanıcı var mı kontrolü
            if (user == null)
                return BadRequest("Geçersiz e-posta veya şifre.");

            // 2. Veritabanındaki şifre boş mu kontrolü (Eski hesaplar için)
            if (string.IsNullOrEmpty(user.Password))
                return BadRequest("Bu hesabın şifresi geçersiz. Lütfen yeni bir hesap oluşturun.");

            // 3. Şifre doğrulama (Hata verirse çökmesini engellemek için Try-Catch içinde)
            try
            {
                bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.Password);
                if (!isPasswordValid)
                    return BadRequest("Geçersiz e-posta veya şifre.");
            }
            catch (System.ArgumentException)
            {
                // Eğer veritabanındaki şifre şifrelenmemiş (eski) ise buraya düşer
                return BadRequest("Eski/Güvensiz bir hesapla giriş yapmaya çalışıyorsunuz. Lütfen yeni hesap açın.");
            }

            // Giriş başarılıysa
            return Ok(user);
        }

        [HttpPut("update-info/{id}")]
        public async Task<IActionResult> UpdateProfileInfo(int id, [FromBody] UserProfileUpdateDto request)
        {
            // 1. Kullanıcıyı veritabanında bul
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound("Kullanıcı bulunamadı.");

            // 2. Gelen yeni verilerle eski verileri değiştir
            user.FullName = request.FullName;
            user.PhoneNumber = request.PhoneNumber;
            user.Height = request.Height;
            user.Weight = request.Weight;
            user.BloodType = request.BloodType;
            user.MedicalConditions = request.MedicalConditions;
            user.Allergies = request.Allergies;
            user.Medications = request.Medications;
            user.OrganStatus = request.OrganStatus;
            user.OrganDetails = request.OrganDetails;
            user.AlcoholUse = request.AlcoholUse;
            user.SmokingHabit = request.SmokingHabit;

            // 3. Kaydet
            await _context.SaveChangesAsync();
            return Ok(new { message = "Profil başarıyla güncellendi!" });
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
        public class UpdateTokenDto
        {
            public int UserId { get; set; }
            public string Token { get; set; }
        }

    }
}