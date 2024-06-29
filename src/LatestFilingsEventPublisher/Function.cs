using Amazon.Lambda.Core;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SimpleSystemsManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using System.Xml.Serialization;
using static LatestFilingsEventPublisher.FeedModel;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
namespace LatestFilingsEventPublisher;

public class Function
{
    public const string SNS_TOPIC_ARN = "SnsTopicArn-YouCouldGetThisFromConfigOrParameterStore";
    private const string RSS_HTTP_CLIENT_NAME = "SecRssClient";
    internal int MAXIMUM_EVENTS_TO_PUBLISH_PER_INVOCATION = 1000;
    internal const int EVENTS_TO_FETCH_PER_REQUEST = 100;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<Function> _logger;
    private readonly IAmazonSimpleNotificationService _snsClient;
    private readonly ILambdaRunTimestampManager _lambdaRunTimestampManager;
    private readonly XmlSerializer _xmlSerializer;

    /// <summary>
    /// Mechanism to get services from the DI container. Marked as internal for testing purposes and enabled by AssemblyAttribute in the project file.
    /// </summary>
    internal IServiceProvider ServiceProvider { get; }

    // <summary>
    // Required default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
    // the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
    // region the Lambda function is executed in.
    // </summary>
    public Function() : this(null)
    {
    }

    /// <summary>
    /// The constructor used for DI and testing.
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <param name="logger"></param>
    public Function(ServiceCollection? serviceCollection = null, ILogger<Function>? logger = null)
    {
        serviceCollection ??= [];

        serviceCollection.AddLogging(builder =>
        {
            builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
            builder.AddConsole();
        });

        serviceCollection.AddHttpClient(RSS_HTTP_CLIENT_NAME, client =>
        {
            client.DefaultRequestHeaders.Host = "www.sec.gov";
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (none email@example.org)");
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        });

        serviceCollection.TryAddSingleton<IAmazonSimpleNotificationService>(serviceProvider =>
        {
            return new AmazonSimpleNotificationServiceClient(new AmazonSimpleNotificationServiceConfig { RegionEndpoint = Amazon.RegionEndpoint.USEast1 });
        });

        serviceCollection.TryAddSingleton<IAmazonSimpleSystemsManagement>(serviceProvider =>
        {
            return new AmazonSimpleSystemsManagementClient(new AmazonSimpleSystemsManagementConfig { RegionEndpoint = Amazon.RegionEndpoint.USEast1 });
        });

        serviceCollection.TryAddSingleton<ILambdaRunTimestampManager, LastRunTimestampManager>();
        serviceCollection.TryAddSingleton(new XmlSerializer(typeof(Feed)));

        ServiceProvider = serviceCollection.BuildServiceProvider();

        _httpClientFactory = ServiceProvider.GetRequiredService<IHttpClientFactory>();
        _logger = logger ?? ServiceProvider.GetRequiredService<ILogger<Function>>();
        _snsClient = ServiceProvider.GetRequiredService<IAmazonSimpleNotificationService>();
        _lambdaRunTimestampManager = ServiceProvider.GetRequiredService<ILambdaRunTimestampManager>();
        _xmlSerializer = ServiceProvider.GetRequiredService<XmlSerializer>();
    }

    /// <summary>
    /// Poll the SEC RSS feed for the latest filings and publish them to an SNS topic.
    /// </summary>
    /// <param name="context">The <see cref="ILambdaContext"/>.</param>
    /// <returns>An awaitable task.</returns>
    public async Task FunctionHandler(ILambdaContext context)
    {
        using (_logger.BeginScope("{AwsRequestId}", context.AwsRequestId))
        {
            // The rate limit is 10 requests per second which is more than enough to keep up with the feed as long as this Lambda is running frequently.
            // You could add a delay here to avoid hitting the rate limit, but you would probably be better off using a different strategy to crawl the SEC files.
            if (MAXIMUM_EVENTS_TO_PUBLISH_PER_INVOCATION / EVENTS_TO_FETCH_PER_REQUEST > 10)
            {
                throw new InvalidOperationException("The SEC endpoint rate limit is 10 requests per second. The maximum number of events to publish per invocation may be too high.");
            }

            string baseUrl = "https://www.sec.gov/cgi-bin/browse-edgar?action=getcurrent&CIK=&type=&company=&dateb=&owner=include&start={0}&count={1}&output=atom";
            var httpClient = _httpClientFactory.CreateClient(RSS_HTTP_CLIENT_NAME);
            var lastRunTimestamp = await _lambdaRunTimestampManager.GetLastRunTimestampAsync();
            _logger.LogInformation("Last run timestamp: {LastRunTimestamp}", lastRunTimestamp);

            var entriesPublished = 0;

            // There are two ways to iterate through the feed: from 0 to maximum, or by paging in reverse order.
            // Paging in reverse order is the safest approach because you can update the last run timestamp and be sure that you won't publish duplicates in the event of a lambda failure,
            // but this means making a minimum of (MAXIMUM_EVENTS_TO_PUBLISH_PER_INVOCATION / EVENTS_TO_FETCH_PER_REQUEST) per invocation since we can only filter the result.
            // Paging from 0 to maximum is more efficient because you can stop as soon as you reach the last run timestamp,
            // but in the event of a failure you will either publish duplicates or miss entries.
            for (int start = 0; start < MAXIMUM_EVENTS_TO_PUBLISH_PER_INVOCATION; start += EVENTS_TO_FETCH_PER_REQUEST)
            {
                string url = string.Format(baseUrl, start, EVENTS_TO_FETCH_PER_REQUEST);
                HttpResponseMessage response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string result = await response.Content.ReadAsStringAsync();
                using TextReader reader = new StringReader(result);

                if (_xmlSerializer.Deserialize(reader) is not Feed deserializedFeed || deserializedFeed.Entries == null || deserializedFeed.Entries.Length == 0)
                {
                    break;
                }

                // The entries' updated timestamps are deserialized in local time.
                var entries = deserializedFeed.Entries.Where(x => x.Updated > lastRunTimestamp.LocalDateTime);
                if (!entries.Any())
                {
                    break;
                }

                entriesPublished += entries.Count();

                foreach (var entry in entries)
                {
                    await _snsClient.PublishAsync(new PublishRequest
                    {
                        TopicArn = SNS_TOPIC_ARN,
                        Message = JsonSerializer.Serialize(entry)
                    });
                }

                // If we get less than the requested count, we have reached the end of the feed.
                if (entries.Count() < EVENTS_TO_FETCH_PER_REQUEST)
                {
                    break;
                }
            }

            // A lambda function error is not one that can be handled in a try/catch. If an error occurs, the function may be retried.
            // Depending on the use case consider setting this at the beginning of processing, or modifying the iterator logic to account for this.
            await _lambdaRunTimestampManager.SetLastRunTimestampAsync(DateTimeOffset.UtcNow);

            _logger.LogInformation("Finished processing feed and published {EntriesPublished} entries.", entriesPublished);
        }
    }
}
