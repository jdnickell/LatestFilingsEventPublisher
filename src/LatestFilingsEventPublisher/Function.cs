using Amazon.Lambda.Core;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using System.Net;
using System.Text.Json;
using System.Xml.Serialization;
using static LatestFilingsEventPublisher.FeedModel;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace LatestFilingsEventPublisher;
public class Function
{
    // TODO: Get from configuration.
    public const string SNS_TOPIC_ARN = "TODOYourSNSTopicArn";

    /// <summary>
    /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
    /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
    /// region the Lambda function is executed in.
    /// </summary>
    public Function()
    {

    }

    /// <summary>
    /// Poll the SEC RSS feed for the latest filings and publish them to an SNS topic.
    /// </summary>
    /// <param name="context">The <see cref="ILambdaContext"/>.</param>
    /// <returns>An awaitable task.</returns>
    public async Task FunctionHandler(ILambdaContext context)
    {
        // TODO: Last run logic - Compare to a stored 'last run time' to avoid publishing events new filings that have already been published, return if there are no new filings.
        string url = "https://www.sec.gov/cgi-bin/browse-edgar?action=getcurrent&CIK=&type=&company=&dateb=&owner=include&start=0&count=40&output=atom";

        HttpClient httpClient = new(new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        });
        httpClient.DefaultRequestHeaders.Host = "www.sec.gov";
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (None jdnickell@gmail.com)");

        // TODO: Needs to page result.
        HttpResponseMessage response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        string result = await response.Content.ReadAsStringAsync();

        var serializer = new XmlSerializer(typeof(Feed));
        using TextReader reader = new StringReader(result);


        if (serializer.Deserialize(reader) is not FeedModel.Feed deserializedFeed || deserializedFeed.Entries.Length == 0)
            return;

        AmazonSimpleNotificationServiceClient snsClient = new();

        // TODO: Last run logic - Store the last run time to avoid publishing events new filings that have already been published.
        var feedUpdated = deserializedFeed.Updated;
        context.Logger.LogInformation($"Feed last updated: {feedUpdated}");
        context.Logger.LogInformation($"Publishing {deserializedFeed.Entries.Length} new filings.");

        foreach (var entry in deserializedFeed.Entries)
        {
            context.Logger.LogInformation($"New filing published: {entry.Title}.");
            await snsClient.PublishAsync(new PublishRequest
            {
                TopicArn = SNS_TOPIC_ARN,
                Message = JsonSerializer.Serialize(entry)
            });
        }

        // TODO: Error handling
    }
}
