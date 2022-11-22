using AutoMapper;
using Microsoft.AspNetCore.Http;
using System;
using System.Security.Claims;

namespace eAgenda.Webapi.Config.AutoMapperConfig
{
    public class UsuarioResolver : IValueResolver<object, object, Guid>
    {
        private readonly IHttpContextAccessor httpContextAccessor;

        public UsuarioResolver(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        public Guid Resolve(object source, object destination, Guid destMember, ResolutionContext context)
        {
            var id = httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(id))
                throw new InvalidOperationException("O Id do usuario não foi encontrado no token");

            return Guid.Parse(id);
        }
    }

}
