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

        [HttpPost("add")]
        public async Task<IActionResult> AddHelper(HelperDto request)
        {
            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null) return NotFound("Kullanıcı bulunamadı.");

            var helperUser = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == request.HelperPhoneNumber);
            if (helperUser == null) return NotFound("Bu numaraya ait kullanıcı bulunamadı.");

            if (user.Id == helperUser.Id) return BadRequest("Kendinizi ekleyemezsiniz.");

            var existing = await _context.Helpers
                .FirstOrDefaultAsync(h => h.UserId == user.Id && h.HelperId == helperUser.Id);

            if (existing != null) return BadRequest("Bu kişi zaten listenizde veya istek gönderilmiş.");

            var newHelper = new Helper
            {
                UserId = user.Id,
                HelperId = helperUser.Id,
                IsVerified = false,      
                Status = "Pending",     
                CreatedAt = DateTime.Now
            };

            _context.Helpers.Add(newHelper);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Arkadaşlık isteği gönderildi." });
        }

        [HttpGet("requests/{myUserId}")]
        public async Task<IActionResult> GetPendingRequests(int myUserId)
        {
            var requests = await _context.Helpers
                .Include(h => h.User) 
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

        [HttpPost("respond")]
        public async Task<IActionResult> RespondToRequest([FromBody] RespondDto request)
        {
            var record = await _context.Helpers.FindAsync(request.RequestId);
            if (record == null) return NotFound("İstek bulunamadı.");

            if (request.Accept)
            {
                record.IsVerified = true;
                record.Status = "Accepted";
                await _context.SaveChangesAsync();
                return Ok(new { message = "İstek kabul edildi." });
            }
            else
            {
                _context.Helpers.Remove(record);
                await _context.SaveChangesAsync();
                return Ok(new { message = "İstek reddedildi ve silindi." });
            }
        }
        
        [HttpGet("contacts/{userId}")]
        public async Task<IActionResult> GetContacts(int userId)
        {
            var contacts = await _context.Helpers
                .Include(h => h.User)
                .Include(h => h.HelperUser) 
                .Where(h => (h.UserId == userId || h.HelperId == userId) && h.Status == "Accepted")
                .Select(h => new
                {
                    Name = h.UserId == userId ? h.HelperUser.FullName : h.User.FullName,
                    Id = h.UserId == userId ? h.HelperId : h.UserId,
                    PhoneNumber = h.UserId == userId ? h.HelperUser.PhoneNumber : h.User.PhoneNumber,
                    BloodType = h.UserId == userId ? h.HelperUser.BloodType : h.User.BloodType,
                    BirthDate = h.UserId == userId ? h.HelperUser.BirthDate : h.User.BirthDate,
                    IsSosActive = h.UserId == userId ? h.HelperUser.IsSosActive : h.User.IsSosActive,
                    LastStatusUpdate = h.UserId == userId ? h.HelperUser.LastStatusUpdate : h.User.LastStatusUpdate,
                    MedicalConditions = h.UserId == userId ? h.HelperUser.MedicalConditions : h.User.MedicalConditions,
                    Allergies = h.UserId == userId ? h.HelperUser.Allergies : h.User.Allergies, 
                    Latitude = h.UserId == userId ? h.HelperUser.Latitude : h.User.Latitude,
                    Longitude = h.UserId == userId ? h.HelperUser.Longitude : h.User.Longitude,
                    Medications = h.UserId == userId ? h.HelperUser.Medications : h.User.Medications,
                    OrganStatus = h.UserId == userId ? h.HelperUser.OrganStatus : h.User.OrganStatus,
                    AlcoholUse = h.UserId == userId ? h.HelperUser.AlcoholUse : h.User.AlcoholUse,
                    SmokingHabit = h.UserId == userId ? h.HelperUser.SmokingHabit : h.User.SmokingHabit,
                    IAllowedThem = h.UserId == userId ? h.UserAllowsHelperToView : h.HelperAllowsUserToView,
                    TheyAllowedMe = h.UserId == userId ? h.HelperAllowsUserToView : h.UserAllowsHelperToView,
                    Height = h.UserId == userId ? h.HelperUser.Height : h.User.Height,
                    Weight = h.UserId == userId ? h.HelperUser.Weight : h.User.Weight,
                    Surgeries = h.UserId == userId ? h.HelperUser.Surgeries : h.User.Surgeries,
                    OrganDetails = h.UserId == userId ? h.HelperUser.OrganDetails : h.User.OrganDetails,
                })
                .ToListAsync();

            return Ok(contacts);
        }
        [HttpPost("UpdateMedicalIdPermission")]
        public IActionResult UpdateMedicalIdPermission([FromQuery] int currentUserId, [FromQuery] int contactId, [FromQuery] bool isAllowed)
        {
            var helperRecord = _context.Helpers.FirstOrDefault(h =>
                (h.UserId == currentUserId && h.HelperId == contactId) ||
                (h.UserId == contactId && h.HelperId == currentUserId));

            if (helperRecord != null)
            {
                if (helperRecord.UserId == currentUserId)
                {
                    helperRecord.UserAllowsHelperToView = isAllowed;
                }
                else if (helperRecord.HelperId == currentUserId)
                {
                    helperRecord.HelperAllowsUserToView = isAllowed;
                }

                _context.SaveChanges();

                return Ok(new { success = true, message = "İzin başarıyla güncellendi." });
            }

            return BadRequest("Kişi bağlantısı bulunamadı.");
        }

    } 
}