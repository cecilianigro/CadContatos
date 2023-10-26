using CadastroContatos.Models;
using Microsoft.EntityFrameworkCore;

namespace CadastroContatos.Data
{
    public class MinimalContextDb : DbContext
    {
        public MinimalContextDb(DbContextOptions<MinimalContextDb> options) : base(options) { }

        public DbSet<Contato> Contatos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Contato>()
                .HasKey(p => p.Id);


            modelBuilder.Entity<Contato>()
                .Property(p => p.Nome)
                .IsRequired()
                .HasColumnType("varchar(200)");

            modelBuilder.Entity<Contato>()
                .Property(p => p.Telefone)
                .IsRequired()
                .HasColumnType("varchar(14)");

            modelBuilder.Entity<Contato>()
               .Property(p => p.TipoContato)
               .IsRequired()
               .HasColumnType("char(1)");

            modelBuilder.Entity<Contato>()
                .ToTable("Contatos");

            base.OnModelCreating(modelBuilder);
        }
    }
}
