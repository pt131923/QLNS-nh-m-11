using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Entities;

namespace API.Interfaces
{
    public interface ITrainingRepository
    {
        Task<IEnumerable<Training>> GetTrainingsAsync();
        Task<Training> GetTrainingByIdAsync(int id);
        Task<bool> SaveAllAsync();

        void Add(Training training);
        void Update(Training training);
        void Delete(Training training);
    }
}