using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization; // YENİ: Güvenlik kütüphanesi
using SafeGuard.Data;
using SafeGuard.Models;
using SafeGuard.Dtos;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SafeGuard.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public UsersController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // POST: api/users/register (Kayıt Ol)
        [HttpPost("register")]
        public async Task<ActionResult<User>> Register(UserRegisterDto request)
        {
            var newUser = new User
            {
                Username = request.Username,
                Email = request.Email,
                Password = request.Password,
                Role = "User"
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok("Kullanıcı başarıyla oluşturuldu.");
        }

        // POST: api/users/login (Giriş Yap -> Token Al)
        [HttpPost("login")]
        public async Task<ActionResult<string>> Login(UserLoginDto request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null || user.Password != request.Password)
            {
                return BadRequest("E-posta veya şifre hatalı.");
            }

            string token = CreateToken(user);
            return Ok(token);
        }

        // GET: api/users/profile (SADECE GİRİŞ YAPAN GÖREBİLİR)
        [HttpGet("profile")]
        [Authorize] // <-- DİKKAT: Bu satır "Sadece Token'ı olan girsin" demek!
        public async Task<ActionResult<User>> GetProfile()
        {
            // Token içindeki ID bilgisini oku
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            // Veritabanından o kişiyi bul
            var user = await _context.Users.FindAsync(userId);

            return Ok(user);
        }

        private string CreateToken(User user)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetSection("JwtSettings:SecretKey").Value!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}