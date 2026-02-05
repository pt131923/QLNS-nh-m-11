using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using API.DTOs;
using AutoMapper;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecuimentController(IRecuimentRepository _recuimentRepository, AutoMapper.IMapper _mapper) : BaseApiController
    {
        // GET api/recuiment
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RecuimentDto>>> GetRecuiments()
        {
            var recuiments = await _recuimentRepository.GetRecuimentsAsync();

            var recuimentDtos = _mapper.Map<IEnumerable<RecuimentDto>>(recuiments);

            return Ok(recuimentDtos);
        }

        // GET api/recuiment/5
        [HttpGet("{id}", Name = "GetRecuimentById")]
        public async Task<ActionResult<RecuimentDto>> GetRecuimentById(int id)
        {
            var recuiment = await _recuimentRepository.GetRecuimentByIdAsync(id);

            if (recuiment == null) return NotFound();

            return Ok(_mapper.Map<RecuimentDto>(recuiment));
        }

        // POST api/recuiment
        [HttpPost("add-recuiment")]
        public async Task<ActionResult<RecuimentDto>> AddRecuiment(RecuimentDto recuimentDto)
        {
            var recuiment = _mapper.Map<Recuiment>(recuimentDto);

            _recuimentRepository.Add(recuiment);

            if (await _recuimentRepository.SaveAllAsync())
            {
                return CreatedAtRoute(
                    "GetRecuimentById",
                    new { id = recuiment.Id },
                    _mapper.Map<RecuimentDto>(recuiment)
                );
            }

            return BadRequest("Failed to create recuiment");
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateRecuiment(int id, RecuimentDto recuimentDto)
        {
            var recuiment = await _recuimentRepository.GetRecuimentByIdAsync(id);

            if (recuiment == null) return NotFound();

            _mapper.Map(recuimentDto, recuiment);
            _recuimentRepository.Update(recuiment);

            if (await _recuimentRepository.SaveAllAsync()) return NoContent();

            return BadRequest("Failed to update recuiment");
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteRecuiment(int id)
        {
            var recuiment = await _recuimentRepository.GetRecuimentByIdAsync(id);

            if (recuiment == null) return NotFound();

            _recuimentRepository.Delete(recuiment);

            if (await _recuimentRepository.SaveAllAsync()) return NoContent();

            return BadRequest("Failed to delete recuiment");
        }
    }
}
