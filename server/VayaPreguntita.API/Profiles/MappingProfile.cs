namespace VayaPreguntita.API.Profiles;

using System.Linq;
using AutoMapper;
using VayaPreguntita.API.DTOs;
using VayaPreguntita.API.DTOs.Groups;
using VayaPreguntita.API.Entities;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // --- Input Mappings (DTO to Entity) ---

        // Groups
        CreateMap<Group, GroupResponse>()
            .ForMember(
                dest => dest.CreatorUsername,
                opt => opt.MapFrom(src => src.Creator.Username)
            );

        CreateMap<CreateQuestionDto, Question>()
            .ForMember(dest => dest.DateCreated, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.Metadata, opt => opt.MapFrom<MetadataResolver>());

        CreateMap<CreateOptionDto, Option>();

        CreateMap<CreateVoteDto, Vote>()
            .ForMember(dest => dest.DateResponded, opt => opt.MapFrom(src => DateTime.UtcNow));

        // --- Output Mappings (Entity to DTO) ---

        CreateMap<Option, OptionDto>();
        CreateMap<Question, QuestionToVoteDto>()
            .ForMember(
                dest => dest.AllowNobody,
                opt => opt.MapFrom(src => src.Metadata.AllowNobody)
            )
            .ForMember(
                dest => dest.BlacklistedUserIds,
                opt => opt.MapFrom(src => src.Metadata.BlacklistedUserIds)
            )
            .ForMember(
                dest => dest.MinSelections,
                opt =>
                    opt.MapFrom(src =>
                        src.Metadata.MinSelections == 0 ? (int?)null : src.Metadata.MinSelections
                    )
            )
            .ForMember(
                dest => dest.MaxSelections,
                opt =>
                    opt.MapFrom(src =>
                        src.Metadata.MaxSelections == 0 ? (int?)null : src.Metadata.MaxSelections
                    )
            )
            .ForMember(dest => dest.RangeMin, opt => opt.MapFrom(src => src.Metadata.RangeMin))
            .ForMember(dest => dest.RangeMax, opt => opt.MapFrom(src => src.Metadata.RangeMax))
            .ForMember(
                dest => dest.TargetUserId,
                opt => opt.MapFrom(src => src.Metadata.TargetUserId)
            )
            .ForMember(
                dest => dest.Teams,
                opt =>
                    opt.MapFrom(src => src.Metadata.Teams.Select(team => team.MemberIds).ToList())
            );

        // Basic user info mapping
        CreateMap<User, UserDto>();

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
