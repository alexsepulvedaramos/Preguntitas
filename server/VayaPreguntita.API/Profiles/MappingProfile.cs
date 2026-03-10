namespace VayaPreguntita.API.Profiles;

using AutoMapper;
using VayaPreguntita.API.DTOs;
using VayaPreguntita.API.Entities;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<CreateQuestionDto, Question>()
            .ForMember(dest => dest.DateCreated, opt => opt.MapFrom(src => DateTime.UtcNow));
        CreateMap<CreateOptionDto, Option>();
        CreateMap<CreateVoteDto, Vote>()
            .ForMember(dest => dest.DateResponded, opt => opt.MapFrom(src => DateTime.UtcNow));
        CreateMap<Option, OptionDto>();
        CreateMap<Question, QuestionToVoteDto>();
    }
}
