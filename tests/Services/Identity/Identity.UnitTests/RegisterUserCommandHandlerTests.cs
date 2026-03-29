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

public class RegisterUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IPasswordHashingService> _passwordHasherMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly RegisterUserCommandHandler _handler;

    public RegisterUserCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _passwordHasherMock = new Mock<IPasswordHashingService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new RegisterUserCommandHandler(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _unitOfWorkMock.Object
        );
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldRegisterUser()
    {
        // Arrange
        var command = new RegisterUserCommand("test@test.com", "Password123", "TestUser");
        _userRepositoryMock.Setup(repo => repo.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
            
        _passwordHasherMock.Setup(h => h.HashPassword(It.IsAny<string>()))
            .Returns("hashedPassword");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _userRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithExistingEmail_ShouldThrowException()
    {
        // Arrange
        var command = new RegisterUserCommand("test@test.com", "Password123", "TestUser");
        
        // Mock a user
        _passwordHasherMock.Setup(h => h.HashPassword(It.IsAny<string>())).Returns("hash");
        var existingUser = User.Register("test@test.com", "OldPassword", "Existing", _passwordHasherMock.Object);
        
        _userRepositoryMock.Setup(repo => repo.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));
    }
}
