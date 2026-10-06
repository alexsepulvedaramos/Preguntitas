namespace VayaPreguntita.API.Profiles;

using System.Linq;
using AutoMapper;
using VayaPreguntita.API.DTOs.Auth;
using VayaPreguntita.API.DTOs.Groups;
using VayaPreguntita.API.DTOs.Questions;
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

        CreateMap<GroupMember, GroupMemberDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.UserId))
            .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.User.Username))
            .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => src.User.AvatarUrl))
            .ForMember(dest => dest.FrameColor, opt => opt.MapFrom(src => src.User.FrameColor))
            .ForMember(dest => dest.JoinedAt, opt => opt.MapFrom(src => src.JoinedAt))
            .ForMember(dest => dest.IsAdmin, opt => opt.MapFrom(src => src.IsAdmin))
            .ForMember(dest => dest.CurrentStreak, opt => opt.Ignore()) // streak, crown and title set by the service
            .ForMember(dest => dest.HasCrown, opt => opt.Ignore())
            .ForMember(dest => dest.Title, opt => opt.Ignore());

        CreateMap<CreateQuestionDto, Question>()
            .ForMember(dest => dest.DateCreated, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.Metadata, opt => opt.MapFrom<MetadataResolver>());

        CreateMap<CreateOptionDto, Option>();

        CreateMap<CreateVoteDto, Vote>()
            .ForMember(dest => dest.DateResponded, opt => opt.MapFrom(src => DateTime.UtcNow));

        // --- Output Mappings (Entity to DTO) ---

        CreateMap<Option, OptionDto>();
        CreateMap<Question, QuestionDto>()
            .ForMember(dest => dest.Teams, opt => opt.MapFrom(src => src.Metadata.Teams));

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
            .ForMember(dest => dest.AllowOther, opt => opt.MapFrom(src => src.Metadata.AllowOther))
            .ForMember(dest => dest.Teams, opt => opt.MapFrom(src => src.Metadata.Teams));

        // Basic user info mapping
        CreateMap<User, UserDto>();

        // Bridge mapping: Extracts user info from a Vote entity
        CreateMap<Vote, VoterDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.UserId))
            .ForMember(
                dest => dest.Username,
                opt => opt.MapFrom(src => src.User != null ? src.User.Username : "Unknown User")
            )
            .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => src.User != null ? src.User.AvatarUrl : null))
            .ForMember(dest => dest.FrameColor, opt => opt.MapFrom(src => src.User != null ? src.User.FrameColor : null));

        // Result mapping: Handled partially by Mapper, partially by Manual Logic
        CreateMap<Option, OptionResultDto>()
            .ForMember(dest => dest.DisplayText, opt => opt.MapFrom(src => src.Text))
            .ForMember(dest => dest.VoteCount, opt => opt.Ignore())
            .ForMember(dest => dest.Percentage, opt => opt.Ignore())
            .ForMember(dest => dest.Voters, opt => opt.Ignore())
            .ForMember(dest => dest.TeamMembers, opt => opt.Ignore());

        // Mapping for the parent Result DTO
        CreateMap<Question, QuestionResultDto>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type))
            .ForMember(dest => dest.Results, opt => opt.Ignore());
    }
}
