using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eAgenda.Dominio.ModuloAutenticacao
{
    public class Usuario: IdentityUser<Guid>
    {
        public string nome { get; set; }
    }
}
