using System;
using System.Threading;
using System.Threading.Tasks;
using Identity.Domain;
using Shared.BuildingBlocks;

namespace Identity.Application.Queries;

public record UserProfileDto(Guid Id, string Email, string DisplayName, DateTimeOffset CreatedAt);

public record GetUserProfileQuery(Guid UserId) : IQuery<UserProfileDto>;

public class GetUserProfileQueryHandler : MediatR.IRequestHandler<GetUserProfileQuery, UserProfileDto>
{
    private readonly IUserRepository _userRepository;

    public GetUserProfileQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserProfileDto> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
            throw new InvalidOperationException("User not found.");

        return new UserProfileDto(user.Id, user.Email, user.DisplayName, user.CreatedAt);
    }
}
