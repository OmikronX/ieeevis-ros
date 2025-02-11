using Microsoft.AspNetCore.Mvc.RazorPages;
using IeeeVisRunOfShowWebApp.Data;
using IeeeVisRunOfShowWebApp.Models;
using Microsoft.EntityFrameworkCore;

namespace IeeeVisRunOfShowWebApp.Pages
{
    public class TestTableModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public TestTableModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<TestTable> TestTableData { get; set; }

        public async Task OnGetAsync()
        {
            TestTableData = await _context.TestTable.ToListAsync();
        }
    }
}
