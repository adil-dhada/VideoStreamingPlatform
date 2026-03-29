using Identity.Domain;

namespace Identity.Application.Abstractions;

public record JwtTokenDto(string AccessToken, System.DateTime ExpiresAt);

public interface IJwtTokenService
{
    JwtTokenDto GenerateToken(User user);
}
