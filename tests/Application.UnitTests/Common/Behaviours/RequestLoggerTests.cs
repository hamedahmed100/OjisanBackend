using OjisanBackend.Application.Common.Behaviours;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Application.Groups.Commands.CreateGroup;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace OjisanBackend.Application.UnitTests.Common.Behaviours;

public class RequestLoggerTests
{
    private Mock<ILogger<CreateGroupCommand>> _logger = null!;
    private Mock<IUser> _user = null!;
    private Mock<IIdentityService> _identityService = null!;

    [SetUp]
    public void Setup()
    {
        _logger = new Mock<ILogger<CreateGroupCommand>>();
        _user = new Mock<IUser>();
        _identityService = new Mock<IIdentityService>();
    }

    [Test]
    public async Task ShouldCallGetUserNameAsyncOnceIfAuthenticated()
    {
        _user.Setup(x => x.Id).Returns(Guid.NewGuid().ToString());

        var requestLogger = new LoggingBehaviour<CreateGroupCommand>(_logger.Object, _user.Object, _identityService.Object);

        await requestLogger.Process(new CreateGroupCommand { MaxMembers = 2, ProductId = 1 }, new CancellationToken());

        _identityService.Verify(i => i.GetUserNameAsync(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task ShouldNotCallGetUserNameAsyncOnceIfUnauthenticated()
    {
        var requestLogger = new LoggingBehaviour<CreateGroupCommand>(_logger.Object, _user.Object, _identityService.Object);

        await requestLogger.Process(new CreateGroupCommand { MaxMembers = 2, ProductId = 1 }, new CancellationToken());

        _identityService.Verify(i => i.GetUserNameAsync(It.IsAny<string>()), Times.Never);
    }
}
