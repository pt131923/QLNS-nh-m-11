using Microsoft.AspNetCore.Mvc;
using API.Entities;
using MongoDB.Driver;
using API.Services;
using System;
using MongoDB.Bson;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContactHistoryController(IMongoCollection<ContactHistory> _history, IMongoIdGenerator _idGenerator) : BaseApiController
    {
        private readonly IMongoCollection<ContactHistory> _history = _history;
        private readonly IMongoIdGenerator _idGenerator = _idGenerator;

        // GET: api/ContactHistory
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _history.Find(_ => true)
                .SortByDescending(x => x.CreatedAt)
                .ToListAsync();
            return Ok(list);
        }
        
        // GET: api/ContactHistory/search?keyword=abc&page=1&pageSize=10
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string keyword, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var filter = Builders<ContactHistory>.Filter.Empty;

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var search = keyword.Trim();
                filter = Builders<ContactHistory>.Filter.Or(
                    Builders<ContactHistory>.Filter.Regex(x => x.Content, new BsonRegularExpression(search, "i"))
                );
            }

            var total = await _history.CountDocumentsAsync(filter);
            var items = await _history.Find(filter)
                .SortByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            return Ok(new
            {
                Data = items,
                Total = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)total / pageSize)
            });
        }

        // GET: api/ContactHistory/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var history = await _history.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (history == null)
                return NotFound("Không tìm thấy bản ghi lịch sử");

            return Ok(history);
        }

        // POST: api/ContactHistory
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ContactHistory model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            model.Id = model.Id == 0 
                ? await _idGenerator.NextAsync("ContactHistory") 
                : model.Id;
            
            model.CreatedAt = DateTime.UtcNow;
            model.UpdatedAt = DateTime.UtcNow;

            await _history.InsertOneAsync(model);
            return Ok(model);
        }

        // PUT: api/ContactHistory/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ContactHistory model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existing = await _history.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (existing == null)
                return NotFound("Không tìm thấy bản ghi lịch sử");

            existing.Content = model.Content;
            existing.UpdatedAt = DateTime.UtcNow;

            var result = await _history.ReplaceOneAsync(x => x.Id == id, existing);
            if (result.ModifiedCount == 0)
                return BadRequest("Không có thay đổi nào được lưu");

            return Ok(existing);
        }

        // DELETE: api/ContactHistory/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _history.DeleteOneAsync(x => x.Id == id);
            if (result.DeletedCount == 0)
                return NotFound("Không tìm thấy bản ghi lịch sử");

            return Ok("Xóa thành công");
        }
    }
}
