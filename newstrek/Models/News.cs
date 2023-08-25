using Nest;
using System.ComponentModel.DataAnnotations.Schema;

namespace newstrek.Models
{
    public class News
    {
        public long Id { get; set; }
        [Column(TypeName = "varchar(max)")]
        [PropertyName("URL")]
        public string? URL { get; set; }
        [Column(TypeName = "varchar(max)")]
        [PropertyName("Date")]
        public string? Date { get; set; }
        [Column(TypeName = "varchar(max)")]
        [PropertyName("Title")]
        public string? Title { get; set; }
        [Column(TypeName = "varchar(max)")]
        [PropertyName("Article")]
        public string Article { get; set; } // 超長字串
        [Column(TypeName = "varchar(max)")]
        [PropertyName("Category")]
        public string? Category { get; set; }
        [Column(TypeName = "varchar(max)")]
        [PropertyName("Tag")]
        public string? Tag { get; set; }
    }
}
