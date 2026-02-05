using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Data;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TrainingController(ITrainingRepository _trainingRepository, AutoMapper.IMapper _mapper) : BaseApiController
    {
        [HttpGet]
        public async Task<IActionResult> GetTraining()
        {
            var training = await _trainingRepository.GetTrainingsAsync();
            return Ok(training);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTrainingById(int id)
        {
            var training = await _trainingRepository.GetTrainingByIdAsync(id);
            if (training == null) return NotFound();
            return Ok(training);
        }

        [HttpPost("add-training")]
        public async Task<IActionResult> AddTraining(Training training)
        {
            _trainingRepository.Add(training);
            if (await _trainingRepository.SaveAllAsync()) return Ok(training);
            return BadRequest("Failed to add training");
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTraining(int id, Training training)
        {
            var existingTraining = await _trainingRepository.GetTrainingByIdAsync(id);
            if (existingTraining == null) return NotFound();

            _mapper.Map(training, existingTraining);
            _trainingRepository.Update(existingTraining);

            if (await _trainingRepository.SaveAllAsync()) return NoContent();
            return BadRequest("Failed to update training");
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTraining(int id)
        {
            var existingTraining = await _trainingRepository.GetTrainingByIdAsync(id);
            if (existingTraining == null) return NotFound();

            _trainingRepository.Delete(existingTraining);

            if (await _trainingRepository.SaveAllAsync()) return NoContent();
            return BadRequest("Failed to delete training");
        }
    }
}