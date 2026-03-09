using Microsoft.AspNetCore.Mvc;
using API.Entities;
using MongoDB.Driver;
using API.Services;
using System;

namespace API.Controllers
{
    
    [Route("api/[controller]")]
    [ApiController]
    public class ContactHistoryController : ControllerBase
    {
        private readonly IMongoCollection<ContactHistory> _history;
        private readonly IMongoIdGenerator _idGenerator;

        public ContactHistoryController(IMongoDatabase db, IMongoIdGenerator idGenerator)
        {
            _history = db.GetCollection<ContactHistory>("ContactHistory");
            _idGenerator = idGenerator;
        }


        // GET: api/ContactHistory
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _history.Find(_ => true)
                .SortByDescending(x => x.CreatedAt)
                .ToListAsync();
            return Ok(list);
        }

        // GET: api/ContactHistory/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var history = await _history.Find(x => x.Id == id).FirstOrDefaultAsync();

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
            if (model.Id == 0)
                model.Id = await _idGenerator.NextAsync("ContactHistory");

            await _history.InsertOneAsync(model);

            return Ok(model);
        }
    }
}
