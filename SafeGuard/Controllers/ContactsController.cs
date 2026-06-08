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

        
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetContacts(int userId)
        {
            
            var contacts = await _context.Contacts.Where(c => c.UserId == userId).ToListAsync();

            
            var detailedContacts = new List<object>();

            foreach (var c in contacts)
            {
                
                var targetProfile = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == c.PhoneNumber);

               
                detailedContacts.Add(new
                {
                    Id = targetProfile?.Id ?? 0,
                    Name = c.Name,
                    PhoneNumber = c.PhoneNumber,
                    VerificationStatus = c.VerificationStatus,
                    BloodType = targetProfile?.BloodType,
                    BirthDate = targetProfile?.BirthDate,


                    IsSosActive = true,
                    LastStatusUpdate = targetProfile?.LastStatusUpdate,
                    MedicalConditions = targetProfile?.MedicalConditions,
                    Allergies = targetProfile?.Allergies
                    
                });
            }

            return Ok(detailedContacts);
        }

       
        [HttpPost("add")]
        public async Task<ActionResult<Contact>> AddContact(Contact contact)
        {
           
            var userExists = await _context.Users.AnyAsync(u => u.Id == contact.UserId);
            if (!userExists) return BadRequest("Geçersiz Kullanıcı ID. Lütfen tekrar giriş yapın.");

            
            contact.VerificationStatus = "Pending";
            _context.Contacts.Add(contact);
            await _context.SaveChangesAsync();

            
            var currentUser = await _context.Users.FindAsync(contact.UserId);
            var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == contact.PhoneNumber);

            if (targetUser != null && currentUser != null)
            {
                
                var match = await _context.Contacts.FirstOrDefaultAsync(c =>
                    c.UserId == targetUser.Id && c.PhoneNumber == currentUser.PhoneNumber);

                if (match != null)
                {
                    
                    match.VerificationStatus = "Verified";
                    contact.VerificationStatus = "Verified";
                    await _context.SaveChangesAsync();
                }
            }

            return Ok(contact);
        }

        [HttpDelete("remove")]
        public async Task<IActionResult> RemoveContact([FromQuery] int userId, [FromQuery] int contactId)
        {
            try
            {
                var helperRecord = await _context.Helpers
                    .FirstOrDefaultAsync(h => h.UserId == userId && h.HelperId == contactId);

                if (helperRecord == null)
                {
                    helperRecord = await _context.Helpers
                        .FirstOrDefaultAsync(h => h.UserId == contactId && h.HelperId == userId);

                    if (helperRecord == null)
                    {
                        return NotFound(new { message = $"İlişki bulunamadı! Aranan -> UserId: {userId}, HelperId: {contactId}" });
                    }
                }
                _context.Helpers.Remove(helperRecord);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Kişi başarıyla silindi." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Sunucu hatası: " + ex.Message });
            }
        }

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