using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Identity.Application.Commands;
using Identity.Application.Abstractions;
using Identity.Domain;
using Moq;
using Xunit;

namespace Identity.UnitTests;

public class LoginUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IPasswordHashingService> _passwordHasherMock;
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock;
    private readonly LoginUserCommandHandler _handler;

    public LoginUserCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _passwordHasherMock = new Mock<IPasswordHashingService>();
        _jwtTokenServiceMock = new Mock<IJwtTokenService>();

        _handler = new LoginUserCommandHandler(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _jwtTokenServiceMock.Object
        );
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ShouldReturnToken()
    {
        // Arrange
        var command = new LoginUserCommand("test@test.com", "Password123");
        
        _passwordHasherMock.Setup(h => h.HashPassword(It.IsAny<string>())).Returns("hash");
        var user = User.Register("test@test.com", "Password123", "TestUser", _passwordHasherMock.Object);
        
        _userRepositoryMock.Setup(repo => repo.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
            
        _passwordHasherMock.Setup(h => h.VerifyPassword("Password123", "hash"))
            .Returns(true);
            
        _jwtTokenServiceMock.Setup(s => s.GenerateToken(user))
            .Returns(new JwtTokenDto("valid-token", DateTime.UtcNow.AddMinutes(15)));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.AccessToken.Should().Be("valid-token");
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Handle_WithInvalidEmail_ShouldThrowUnauthorized()
    {
        // Arrange
        var command = new LoginUserCommand("wrong@test.com", "Password123");
        
        _userRepositoryMock.Setup(repo => repo.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithInvalidPassword_ShouldThrowUnauthorized()
    {
        // Arrange
        var command = new LoginUserCommand("test@test.com", "WrongPassword");
        
        _passwordHasherMock.Setup(h => h.HashPassword(It.IsAny<string>())).Returns("hash");
        var user = User.Register("test@test.com", "Password123", "TestUser", _passwordHasherMock.Object);

        _userRepositoryMock.Setup(repo => repo.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
            
        _passwordHasherMock.Setup(h => h.VerifyPassword("WrongPassword", "hash"))
            .Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.Handle(command, CancellationToken.None));
    }
}
