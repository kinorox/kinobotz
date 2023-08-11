using AutoMapper;
using Entities;
using webapi.Dto;

namespace webapi
{
    public class MappingProfiles : Profile
    {
        public MappingProfiles()
        {
            CreateMap<BotConnection, BotConnectionDto>();
        }
    }
}
