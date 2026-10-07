using API.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace API.Controllers
{
    public class BuggyController(IMongoCollection<Employee> _employees) : BaseApiController
    {

        [Authorize]
        [HttpGet("auth")]
        public ActionResult<string> GetSecret()
        {
            return "secret text";
        }

        [HttpGet("not-found")]
        public async Task<ActionResult<Employee>> GetNotFound()
        {
            var thing = await _employees.Find(e => e.EmployeeId == -1).FirstOrDefaultAsync();

            if (thing == null) return NotFound();

            return thing;
        }

        [HttpGet("server-error")]
        public async Task<ActionResult<string>> GetServerError()
        {
            var thing = await _employees.Find(e => e.EmployeeId == -1).FirstOrDefaultAsync();

            var thingToReturn = thing.ToString();

            return thingToReturn;
        }

        [HttpGet("bad-request")]
        public ActionResult<string> GetBadRequest()
        {
            return BadRequest("This was not a good request");
        }
    }
}