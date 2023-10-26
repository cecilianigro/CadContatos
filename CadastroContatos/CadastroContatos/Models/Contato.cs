using System.ComponentModel.DataAnnotations.Schema;

namespace CadastroContatos.Models
{
    [Table("AspNetUsers")]
    public class Contato
    {
        public Guid Id { get; set; }
        public string? Nome { get; set; }
        public string? Telefone { get; set; }
        public string? TipoContato { get; set; }
    }
}
