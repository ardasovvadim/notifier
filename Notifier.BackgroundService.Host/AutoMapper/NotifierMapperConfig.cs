using AutoMapper;
using Notifier.BackgroundService.Host.Contracts.Rezka;
using Notifier.BackgroundService.Host.Database.Entities;

namespace Notifier.BackgroundService.Host.AutoMapper;

public class NotifierMapperConfig : Profile
{
    public NotifierMapperConfig()
    {
        CreateMap<RezkaMovieInfo, MovieRecord>().ReverseMap();
    }
}