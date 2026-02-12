using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeGuard.API.Models;
using SafeGuard.Data;
using SafeGuard.Models;

namespace SafeGuard.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContactsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ContactsController(AppDbContext context)
        {
            _context = context;
        }

        // --- KİŞİLERİ GETİR (api/contacts/1) ---
        [HttpGet("{userId}")]
        public async Task<ActionResult<IEnumerable<Contact>>> GetContacts(int userId)
        {
            return await _context.Contacts.Where(c => c.UserId == userId).ToListAsync();
        }

        // --- KİŞİ EKLE (api/contacts/add) ---
        // DÜZELTİLDİ: "add" rotası eklendi ki servis ile eşleşsin
        [HttpPost("add")]
        public async Task<ActionResult<Contact>> AddContact(Contact contact)
        {
            // 1. Kullanıcı var mı kontrolü (ID hatası almamak için)
            var userExists = await _context.Users.AnyAsync(u => u.Id == contact.UserId);
            if (!userExists) return BadRequest("Geçersiz Kullanıcı ID. Lütfen tekrar giriş yapın.");

            // 2. Önce kişiyi "Pending" (Sarı) olarak ayarla
            contact.VerificationStatus = "Pending";
            _context.Contacts.Add(contact);
            await _context.SaveChangesAsync();

            // --- AKILLI EŞLEŞTİRME MANTIĞI ---
            var currentUser = await _context.Users.FindAsync(contact.UserId);
            var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == contact.PhoneNumber);

            if (targetUser != null && currentUser != null)
            {
                // Karşı tarafın rehberinde ben var mıyım?
                var match = await _context.Contacts.FirstOrDefaultAsync(c =>
                    c.UserId == targetUser.Id && c.PhoneNumber == currentUser.PhoneNumber);

                if (match != null)
                {
                    // Eşleşme bulundu, ikisini de "Verified" (Yeşil) yap
                    match.VerificationStatus = "Verified";
                    contact.VerificationStatus = "Verified";
                    await _context.SaveChangesAsync();
                }
            }

            return Ok(contact);
        }

        // --- KİŞİ SİL ---
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteContact(int id)
        {
            var contact = await _context.Contacts.FindAsync(id);
            if (contact == null) return NotFound();

            _context.Contacts.Remove(contact);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}