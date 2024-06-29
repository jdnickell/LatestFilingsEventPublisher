using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using Moq;
using Xunit;

namespace LatestFilingsEventPublisher.UnitTests;

public class LastRunTimestampManagerTests
{
    /// <summary>
    /// Test that GetLastRunTimestampAsync returns the current datetime if the value in Parameter store is null.
    /// </summary>
    /// <returns>An awaitable Task.</returns>
    [Fact]
    public async Task GetLastRunTimestampAsync_ParameterStoreReturnsNull_ReturnsCurrentDateTime()
    {
        // Arrange
        var client = new Mock<IAmazonSimpleSystemsManagement>();
        client.Setup(x => x.GetParameterAsync(It.IsAny<GetParameterRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetParameterResponse
            {
                Parameter = null
            });

        var lastRunTimestampManager = new LastRunTimestampManager(client.Object);

        // Act
        var lastRunTimestamp = await lastRunTimestampManager.GetLastRunTimestampAsync();
        var now = DateTimeOffset.UtcNow;

        // Assert
        Assert.True(lastRunTimestamp > now.AddMinutes(-1));
        Assert.True(lastRunTimestamp < now.AddMinutes(1));
    }

    /// <summary>
    /// Test that the GetLastRunTimestampAsync method returns the value from the Parameter Store in local time.
    /// </summary>
    /// <returns>An awaitable Task.</returns>
    [Fact]
    public async Task GetLastRunTimestampAsync_ParameterStoreReturnsValue_ReturnsValue()
    {
        // Arrange
        var client = new Mock<IAmazonSimpleSystemsManagement>();
        client.Setup(x => x.GetParameterAsync(It.IsAny<GetParameterRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetParameterResponse
            {
                Parameter = new Parameter
                {
                    Value = "6/28/2024 10:27:52 PM +00:00"
                }
            });
        var lastRunTimestampManager = new LastRunTimestampManager(client.Object);

        // Act
        var result = await lastRunTimestampManager.GetLastRunTimestampAsync();

        // Assert
        Assert.Equal(new DateTime(2024, 6, 28, 17, 27, 52, 000, DateTimeKind.Local), result);
    }

    /// <summary>
    /// Test that the SetLastRunTimestampAsync method puts the parameter value in the correct format.
    /// </summary>
    /// <returns>An awaitable Task.</returns>
    [Fact]
    public async Task SetLastRunTimestampAsync_PutsParameterStringValue()
    {
        // Arrange
        var client = new Mock<IAmazonSimpleSystemsManagement>();
        client.Setup(x => x.PutParameterAsync(It.IsAny<PutParameterRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutParameterResponse
            {
                HttpStatusCode = System.Net.HttpStatusCode.OK
            });

        var lastRunTimestampManager = new LastRunTimestampManager(client.Object);

        // Act
        var timestamp = new DateTimeOffset(new DateTime(2024, 6, 27, 14, 35, 56, 712, DateTimeKind.Utc));
        await lastRunTimestampManager.SetLastRunTimestampAsync(timestamp);

        // Assert
        client.Verify(x => x.PutParameterAsync(It.Is<PutParameterRequest>(r => r.Value == "6/27/2024 2:35:56 PM +00:00"), It.IsAny<CancellationToken>()));
    }

    /// <summary>
    /// Test that the SetLastRunTimestampAsync method throws an exception when the PutParameterAsync call fails.
    /// </summary>
    /// <returns>An awaitable Task.</returns>
    [Fact]
    public async Task WhenPutParameterFails_ThrowsException()
    {
        // Arrange
        var client = new Mock<IAmazonSimpleSystemsManagement>();
        client.Setup(x => x.PutParameterAsync(It.IsAny<PutParameterRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutParameterResponse
            {
                HttpStatusCode = System.Net.HttpStatusCode.BadRequest
            });

        var lastRunTimestampManager = new LastRunTimestampManager(client.Object);

        // Act
        async Task Act() => await lastRunTimestampManager.SetLastRunTimestampAsync(new DateTimeOffset(new DateTime(2024, 6, 27, 14, 35, 56, 712, DateTimeKind.Utc)));

        // Assert
        await Assert.ThrowsAsync<Exception>(Act);
    }
}
