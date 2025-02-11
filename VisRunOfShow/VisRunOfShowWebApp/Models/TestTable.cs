using System.ComponentModel.DataAnnotations;

namespace IeeeVisRunOfShowWebApp.Models
{
    public class TestTable
    {
        [Key]
        public int id { get; set; }

        [Required]
        public string name { get; set; }
    }
}
