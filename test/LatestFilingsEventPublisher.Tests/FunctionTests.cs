using Amazon.Lambda.TestUtilities;
using Amazon.Runtime.SharedInterfaces;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using LatestFilingsEventPublisher.UnitTests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Xml.Serialization;
using Xunit;

namespace LatestFilingsEventPublisher.Tests;

public class FunctionTests
{
    /// <summary>
    /// Tests that the required <see cref="Function"/> services are registered in the DI container.
    /// </summary>
    [Fact]
    public void Function_DependencyInjection_RequiredServicesAreFound()
    {
        // Arrange
        ServiceCollection serviceCollection = new();

        // Act
        var function = new Function(serviceCollection);      

        // Assert
        Assert.NotNull(function.ServiceProvider);
        Assert.NotNull(function.ServiceProvider.GetService(typeof(ILogger<Function>)));
        Assert.NotNull(function.ServiceProvider.GetService(typeof(IHttpClientFactory)));
        Assert.NotNull(function.ServiceProvider.GetService(typeof(IAmazonSimpleNotificationService)));
        Assert.NotNull(function.ServiceProvider.GetService(typeof(XmlSerializer)));
    }

    /// <summary>
    /// Tests the function handler publishes the expected number of events when the feed data is valid.
    /// </summary>
    [Theory]
    [InlineData(TestFeedData.FeedWithNoEntries, 0)]
    [InlineData(TestFeedData.FeedWithTwoEntries, 2)]
    public async void FunctionHandler_ValidFeedData_PublishesExpectedNumberOfEvents(string testFeedData, int expectedPublishedEventsCount)
    {
        // Arrange
        var loggerMock = new Mock<ILogger<Function>>();

        // Setup the HTTP client to return a Feed with the test Theory entries.
        var mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var mockResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(testFeedData)
        };
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(m => m.Method == HttpMethod.Get),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);
        var httpClient = new HttpClient(mockHandler.Object);
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock
            .Setup(_ => _.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        var snsConfigMock = new Mock<AmazonSimpleNotificationServiceConfig>();
        var snsMock = new Mock<IAmazonSimpleNotificationService>();
        snsMock.Setup(sns => sns.PublishAsync(It.IsAny<PublishRequest>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(new PublishResponse { HttpStatusCode = HttpStatusCode.OK });

        ServiceCollection serviceCollection = new();
        serviceCollection.AddSingleton(loggerMock.Object);
        serviceCollection.AddSingleton(httpClientFactoryMock.Object);
        serviceCollection.AddSingleton(snsConfigMock.Object);
        serviceCollection.AddSingleton(snsMock.Object);
        // Don't mock the XML serializer, use the real one.

        var context = new TestLambdaContext();
        var function = new Function(serviceCollection);

        // Act
        var exception = await Record.ExceptionAsync(async () =>
        {
            await function.FunctionHandler(context);
        });

        // Assert
        Assert.Null(exception);
        Assert.True(snsMock.Invocations.Count == expectedPublishedEventsCount);
    }
}
