using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Data;
using API.Entities;
namespace API.Interfaces
{
    public interface IRecuimentRepository
    {
        Task<IEnumerable<Recuiment>> GetRecuimentsAsync();
        Task<Recuiment> GetRecuimentByIdAsync(int id);
        Task<bool> SaveAllAsync();

        void Add(Recuiment recuiment);
        void Update(Recuiment recuiment);

        void Delete(Recuiment recuiment);
    }
}