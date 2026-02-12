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

        // --- KAYIT OL ---
        [HttpPost("register")]
        public async Task<ActionResult<User>> Register(UserDto request)
        {
            if (_context.Users.Any(u => u.Email == request.Email))
                return BadRequest("Bu e-posta adresi zaten kullanılıyor.");

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new User
            {
                FullName = request.FullName,
                Username = request.Username,
                Email = request.Email,
                PasswordHash = passwordHash,
                Role = "User",
                PhoneNumber = request.PhoneNumber ?? "",
                BloodType = request.BloodType ?? "",
                ChronicDiseases = request.ChronicDiseases ?? "",
                Allergies = request.Allergies ?? "",
                Smoker = request.Smoker,
                AlcoholConsumption = request.AlcoholConsumption
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return Ok(user);
        }

        // --- GİRİŞ YAP (DÜZELTİLDİ) ---
        [HttpPost("login")]
        public async Task<ActionResult<User>> Login(UserLoginDto request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null) return BadRequest("Kullanıcı bulunamadı.");

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return BadRequest("Yanlış şifre.");

            // DÜZELTME: Sadece "Ok(user)" diyoruz. İsmi ve ID'yi böyle alacağız.
            return Ok(user);
        }
    }
}