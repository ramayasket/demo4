using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace Demo4.Worker
{
    public class Document
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        public string FileName { get; set; }

        [Required]
        public int FileSize { get; set; }

        [Required]
        public byte[] FileData { get; set; }

        [Required]
        public bool ContentExtracted { get; set; }

        public string? Content { get; set; }
    }

    public class DocumentContext(IConfiguration configuration) : DbContext
    {
        public DbSet<Document> Documents { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            optionsBuilder.UseNpgsql(configuration.GetConnectionString(nameof(DocumentContext))!);
        }
    }
}
