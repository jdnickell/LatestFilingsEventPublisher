using Amazon.Lambda.Core;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
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
    // TODO: Get from configuration or ParameterStore.
    public const string SNS_TOPIC_ARN = "YourSnsTopicArn";
    private const string RSS_HTTP_CLIENT_NAME = "SecRssClient";

    private readonly IHttpClientFactory _httpClientFactory;

    private readonly ILogger<Function> _logger;

    private readonly IAmazonSimpleNotificationService _snsClient;

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

        // Logging
        serviceCollection.AddLogging(builder =>
        {
            builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
            builder.AddConsole();
        });

        // HttpClient
        serviceCollection.AddHttpClient(RSS_HTTP_CLIENT_NAME, client =>
        {
            client.DefaultRequestHeaders.Host = "www.sec.gov";
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (None jdnickell@gmail.com)");
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        });
        // TODO: Add retry policy.

        // SNS
        serviceCollection.TryAddSingleton<IAmazonSimpleNotificationService>(serviceProvider =>
        {
            // Optionally register Xray tracing
            return new AmazonSimpleNotificationServiceClient(new AmazonSimpleNotificationServiceConfig { RegionEndpoint = Amazon.RegionEndpoint.USEast1 });
        });


        // XML Serializer
        serviceCollection.TryAddSingleton(new XmlSerializer(typeof(Feed)));

        ServiceProvider = serviceCollection.BuildServiceProvider();

        _httpClientFactory = ServiceProvider.GetRequiredService<IHttpClientFactory>();
        _logger = logger ?? ServiceProvider.GetRequiredService<ILogger<Function>>();
        _snsClient = ServiceProvider.GetRequiredService<IAmazonSimpleNotificationService>();
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
            // TODO: Last run logic - Compare to a stored 'last run time' to avoid publishing events new filings that have already been published, return if there are no new filings.
            string url = "https://www.sec.gov/cgi-bin/browse-edgar?action=getcurrent&CIK=&type=&company=&dateb=&owner=include&start=0&count=40&output=atom";

            var httpClient = _httpClientFactory.CreateClient(RSS_HTTP_CLIENT_NAME);

            // TODO: Needs to page result.
            HttpResponseMessage response = await httpClient.GetAsync(url);

            response.EnsureSuccessStatusCode();

            string result = await response.Content.ReadAsStringAsync();

            using TextReader reader = new StringReader(result);

            if (_xmlSerializer.Deserialize(reader) is not Feed deserializedFeed || deserializedFeed.Entries == null || deserializedFeed.Entries.Length == 0)
                return;

            // TODO: Last run logic - Store the last run time to avoid publishing events new filings that have already been published.
            var feedUpdated = deserializedFeed.Updated;
            _logger.LogInformation("Feed last updated: {updated}. Publishing {Count} new filings.", feedUpdated, deserializedFeed.Entries.Length);

            foreach (var entry in deserializedFeed.Entries)
            {
                await _snsClient.PublishAsync(new PublishRequest
                {
                    TopicArn = SNS_TOPIC_ARN,
                    Message = JsonSerializer.Serialize(entry)
                });
            }
        }
    }
}
