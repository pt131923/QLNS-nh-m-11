using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API.Entities;

namespace API.Controllers
{
    
    [Route("api/[controller]")]
    [ApiController]
    public class ContactHistoryController(DbContext _context) : ControllerBase
    {


        // GET: api/ContactHistory
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _context.Set<ContactHistory>()
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return Ok(list);
        }

        // GET: api/ContactHistory/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var history = await _context.Set<ContactHistory>().FindAsync(id);

            if (history == null)
                return NotFound();

            return Ok(history);
        }

        // POST: api/ContactHistory
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ContactHistory model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            model.CreatedAt = DateTime.UtcNow;

            _context.Set<ContactHistory>().Add(model);
            await _context.SaveChangesAsync();

            return Ok(model);
        }
    }
}
