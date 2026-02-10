using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // ToListAsync için gerekli
using SafeGuard.Data;
using SafeGuard.Models;

namespace SafeGuard.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        // Veritabanı bağlantısını buraya çağırıyoruz (Constructor Injection)
        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/users (Tüm kullanıcıları getir)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }

        // POST: api/users (Yeni kullanıcı ekle)
        [HttpPost]
        public async Task<ActionResult<User>> PostUser(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUsers), new { id = user.Id }, user);
        }
    }
}