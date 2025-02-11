using Microsoft.AspNetCore.Mvc;
using IeeeVisRunOfShowWebApp.Data;
using IeeeVisRunOfShowWebApp.Models;

namespace IeeeVisRunOfShowWebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestTableController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TestTableController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Get()
        {
            var data = _context.TestTable.ToList();
            return Ok(data);
        }

        [HttpPost]
        public IActionResult Add([FromBody] TestTable newRecord)
        {
            if (newRecord == null)
                return BadRequest("Invalid data.");

            _context.TestTable.Add(newRecord);
            _context.SaveChanges();
            return Ok(newRecord);
        }

        public IActionResult TestTable()
        {
            var data = _context.TestTable.ToList();
            return View(data);
        }
    }
}
