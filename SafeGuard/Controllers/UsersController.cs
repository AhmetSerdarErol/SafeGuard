using Microsoft.AspNetCore.Mvc;
using SafeGuard.Data;
using SafeGuard.Models;

namespace SafeGuard.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        // "Alet Kutusu"ndan veritabanı bağlantısını istiyoruz (Dependency Injection)
        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        // POST: api/users/register
        [HttpPost("register")]
        public IActionResult Register(User user)
        {
            // 1. Yeni kullanıcıyı veritabanı hafızasına ekle
            _context.Users.Add(user);

            // 2. "Kaydet" butonuna bas (Kalıcı hale getir)
            _context.SaveChanges();

            return Ok(new { message = "Kayıt Başarılı!", userId = user.Id });
        }
    }
}