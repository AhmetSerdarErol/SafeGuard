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
    }
}