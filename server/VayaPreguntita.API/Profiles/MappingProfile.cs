namespace VayaPreguntita.API.Profiles;

using AutoMapper;
using VayaPreguntita.API.DTOs;
using VayaPreguntita.API.Entities;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // --- Input Mappings (DTO to Entity) ---

        CreateMap<CreateQuestionDto, Question>()
            .ForMember(dest => dest.DateCreated, opt => opt.MapFrom(src => DateTime.UtcNow));

        CreateMap<CreateOptionDto, Option>();

        CreateMap<CreateVoteDto, Vote>()
            .ForMember(dest => dest.DateResponded, opt => opt.MapFrom(src => DateTime.UtcNow));

        // --- Output Mappings (Entity to DTO) ---

        CreateMap<Option, OptionDto>();
        CreateMap<Question, QuestionToVoteDto>();

        // Basic user info mapping
        CreateMap<User, VoterDto>();

        // Bridge mapping: Extracts user info from a Vote entity
        CreateMap<Vote, VoterDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.UserId))
            // Null check prevents exceptions if .Include(v => v.User) is missing
            .ForMember(
                dest => dest.Username,
                opt => opt.MapFrom(src => src.User != null ? src.User.Username : "Unknown User")
            );

        // Result mapping: Handled partially by Mapper, partially by Manual Logic
        CreateMap<Option, OptionResultDto>()
            .ForMember(dest => dest.DisplayText, opt => opt.MapFrom(src => src.Text))
            .ForMember(dest => dest.VoteCount, opt => opt.Ignore())
            .ForMember(dest => dest.Percentage, opt => opt.Ignore())
            .ForMember(dest => dest.Voters, opt => opt.Ignore());

        // Mapping for the parent Result DTO
        CreateMap<Question, QuestionResultDto>()
            .ForMember(dest => dest.Results, opt => opt.Ignore());
    }
}
