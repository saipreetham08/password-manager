using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace PM.Models
{
    public class FileDB
    {
        [Key]
        [ForeignKey("UserName")]
        public string Id { get; set; }
        public string FileName { get; set; }

    }
}
