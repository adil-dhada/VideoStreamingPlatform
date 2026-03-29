using System;
using System.Threading;
using System.Threading.Tasks;
using Identity.Application.Abstractions;
using Identity.Domain;
using Shared.BuildingBlocks;

namespace Identity.Application.Commands;

public record LoginUserCommand(string Email, string Password) : ICommand<JwtTokenDto>;

public class LoginUserCommandHandler : MediatR.IRequestHandler<LoginUserCommand, JwtTokenDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHashingService _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public LoginUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHashingService passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<JwtTokenDto> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        
        if (user == null || !user.ValidatePassword(request.Password, _passwordHasher))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("User account is disabled.");
        }

        return _jwtTokenService.GenerateToken(user);
    }
}
