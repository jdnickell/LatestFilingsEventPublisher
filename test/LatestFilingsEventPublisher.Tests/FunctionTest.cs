using Amazon.Lambda.TestUtilities;
using Xunit;

namespace LatestFilingsEventPublisher.Tests;

public class FunctionTest
{
    /// <summary>
    /// Tests that the request to get the RSS feed and deserialize the response does not throw an exception.
    /// </summary>
    [Fact]
    public async void FunctionHandler_NoException()
    {
        // Arrange
        var context = new TestLambdaContext();
        var function = new Function();

        // Act
        var exception = await Record.ExceptionAsync(async () =>
        {
            await function.FunctionHandler(context);
        });

        // Assert
        Assert.Null(exception);
    }
}
