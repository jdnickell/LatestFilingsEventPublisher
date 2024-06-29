using Amazon.Lambda.TestUtilities;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SimpleSystemsManagement;
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
        Assert.NotNull(function.ServiceProvider.GetService(typeof(IAmazonSimpleSystemsManagement)));
        Assert.NotNull(function.ServiceProvider.GetService(typeof(ILambdaRunTimestampManager)));
        Assert.NotNull(function.ServiceProvider.GetService(typeof(XmlSerializer)));
    }

    /// <summary>
    /// Tests the function handler publishes the expected number of events when the feed data is valid based on the last run timestamp.
    /// </summary>
    /// <param name="testFeedData">Sample XML feed data in valid format.</param>
    /// <param name="expectedPublishedEventsCount">The number of times we expect to publish an event based on the given feed data.</param>
    /// <param name="lastRunDateTime">Datetime representing the last successful processing time.</param>
    [Theory]
    [InlineData(TestFeedData.FeedWithNoEntries, 0, "3/15/2024 10:27:52 PM +00:00")] // No entries in the feed.
    [InlineData(TestFeedData.FeedWithTwoEntries, 1, "3/15/2024 17:34:10 PM +00:00")] // 2 entries in the feed, but only 1 is newer than the last run timestamp.
    [InlineData(TestFeedData.FeedWithTwoEntries, 2, "3/15/2002 10:27:00 PM +00:00")] // 2 entries in the feed, both newer than the last run timestamp.
    [InlineData(TestFeedData.FeedWithTwoEntries, 0, "1/10/2200 10:27:00 PM +00:00")] // 2 entries in the feed, but both are older than the last run timestamp.
    public async void FunctionHandler_ValidFeedData_PublishesExpectedNumberOfEvents(string testFeedData, int expectedPublishedEventsCount, string lastRunDateTime)
    {
        // Arrange
        var loggerMock = new Mock<ILogger<Function>>();
        var lastRunTimestampManagerMock = new Mock<ILambdaRunTimestampManager>();

        // Set up the HTTP client to return a Feed with the test Theory entries.
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

        // Set up the LastRunTimestampManager to return the test Theory last run timestamp.
        lastRunTimestampManagerMock.Setup(m => m.GetLastRunTimestampAsync())
            .ReturnsAsync(DateTimeOffset.Parse(lastRunDateTime));
        lastRunTimestampManagerMock.Setup(m => m.SetLastRunTimestampAsync(It.IsAny<DateTimeOffset>()));

        // Set up the SNS client to return OK for the PublishAsync method.
        var snsConfigMock = new Mock<AmazonSimpleNotificationServiceConfig>();
        var snsMock = new Mock<IAmazonSimpleNotificationService>();
        snsMock.Setup(sns => sns.PublishAsync(It.IsAny<PublishRequest>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(new PublishResponse { HttpStatusCode = HttpStatusCode.OK });

        ServiceCollection serviceCollection = new();
        serviceCollection.AddSingleton(loggerMock.Object);
        serviceCollection.AddSingleton(httpClientFactoryMock.Object);
        serviceCollection.AddSingleton(lastRunTimestampManagerMock.Object);
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
        Assert.Equal(expectedPublishedEventsCount, snsMock.Invocations.Count);
        Assert.Equal(1, lastRunTimestampManagerMock.Invocations.Count(x => x.Method.Name == nameof(ILambdaRunTimestampManager.SetLastRunTimestampAsync)));
    }

    /// <summary>
    /// When the request to get feed data consistently returns 100 new entries, the function should publish a maximum of <see cref="Function.MAXIMUM_EVENTS_TO_PUBLISH_PER_INVOCATION"/> events.
    /// </summary>
    [Fact]
    public async void FunctionHandler_UnlimitedValidFeedData_PublishesMaximum1000Events()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<Function>>();
        var lastRunTimestampManagerMock = new Mock<ILambdaRunTimestampManager>();

        // Set up the HTTP client to return a Feed with 100 entries from the Test data.
        // This mock will return the same 100 entries each time it's called in the loop to illustrate more than Function.MAXIMUM_EVENTS_TO_PUBLISH_PER_INVOCATION entries are new since the last lambda invocation.
        var mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var mockResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(TestFeedData.FeedWith100Entries)
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

        // Set up the LastRunTimestampManager to return the test Theory last run timestamp.
        lastRunTimestampManagerMock.Setup(m => m.GetLastRunTimestampAsync())
            .ReturnsAsync(DateTimeOffset.Parse("3/15/2000 17:34:10 PM +00:00"));
        lastRunTimestampManagerMock.Setup(m => m.SetLastRunTimestampAsync(It.IsAny<DateTimeOffset>()));

        // Set up the SNS client to return OK for the PublishAsync method.
        var snsConfigMock = new Mock<AmazonSimpleNotificationServiceConfig>();
        var snsMock = new Mock<IAmazonSimpleNotificationService>();
        snsMock.Setup(sns => sns.PublishAsync(It.IsAny<PublishRequest>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(new PublishResponse { HttpStatusCode = HttpStatusCode.OK });

        ServiceCollection serviceCollection = new();
        serviceCollection.AddSingleton(loggerMock.Object);
        serviceCollection.AddSingleton(httpClientFactoryMock.Object);
        serviceCollection.AddSingleton(lastRunTimestampManagerMock.Object);
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
        Assert.Equal(function.MAXIMUM_EVENTS_TO_PUBLISH_PER_INVOCATION, snsMock.Invocations.Count);
        Assert.Equal(1, lastRunTimestampManagerMock.Invocations.Count(x => x.Method.Name == nameof(ILambdaRunTimestampManager.SetLastRunTimestampAsync)));
    }

    /// <summary>
    /// Tests that the function handler throws an exception if the feed data cannot be deserialized so that alarms can be triggered.
    /// </summary>
    [Fact]
    public async void FunctionHandler_InvalidFeedData_ThrowsException()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<Function>>();
        var lastRunTimestampManagerMock = new Mock<ILambdaRunTimestampManager>();

        // Set up the HTTP client to return a Feed with the test Theory entries.
        var mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var mockResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("Invalid shaped XML response.")
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

        // Set up the LastRunTimestampManager to return the test Theory last run timestamp.
        lastRunTimestampManagerMock.Setup(m => m.GetLastRunTimestampAsync())
            .ReturnsAsync(DateTimeOffset.Now);
        lastRunTimestampManagerMock.Setup(m => m.SetLastRunTimestampAsync(It.IsAny<DateTimeOffset>()));

        ServiceCollection serviceCollection = new();
        serviceCollection.AddSingleton(loggerMock.Object);
        serviceCollection.AddSingleton(httpClientFactoryMock.Object);
        serviceCollection.AddSingleton(lastRunTimestampManagerMock.Object);

        var context = new TestLambdaContext();
        var function = new Function(serviceCollection);

        // Act
        var exception = await Record.ExceptionAsync(async () =>
        {
            await function.FunctionHandler(context);
        });

        // Assert
        Assert.NotNull(exception);
    }

    /// <summary>
    /// Using a setting that would exceed the rate limit of the SEC endpoint throws an exception.
    /// </summary>
    [Fact]
    public async void FunctionHandler_SettingsExceedRateLimit_ThrowsException()
    {
        // Arrange
        var context = new TestLambdaContext();
        var function = new Function()
        {
            MAXIMUM_EVENTS_TO_PUBLISH_PER_INVOCATION = 2000
        };

        // Act
        var exception = await Record.ExceptionAsync(async () =>
        {
            await function.FunctionHandler(context);
        });

        // Assert
        Assert.NotNull(exception);
    }
}
