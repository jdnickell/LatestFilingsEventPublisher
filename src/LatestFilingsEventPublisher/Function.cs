using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Xml.Serialization;

namespace LatestFilingsEventPublisher;

public class Function
{
    /// <summary>
    /// The main entry point for the Lambda function. The main function is called once during the Lambda init phase. It
    /// initializes the .NET Lambda runtime client passing in the function handler to invoke for each Lambda event and
    /// the JSON serializer to use for converting Lambda JSON format to the .NET types. 
    /// </summary>
    private static async Task Main()
    {
        await LambdaBootstrapBuilder.Create(FunctionHandler)
            .Build()
            .RunAsync();
    }

    /// <summary>
    /// Work in progress: This function will be invoked by the Lambda runtime for each event and currently deserializes the RSS feed and logs a message.
    /// </summary>
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "The XML serialization required is minimal and has been tested.")]
    public static async Task FunctionHandler(ILambdaContext context)
    {
        string url = "https://www.sec.gov/cgi-bin/browse-edgar?action=getcurrent&CIK=&type=&company=&dateb=&owner=include&start=0&count=40&output=atom";

        HttpClient client = new(new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        });
        client.DefaultRequestHeaders.Host = "www.sec.gov";
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (YourCompany you@example.com)");

        // TODO: Needs to page result.
        HttpResponseMessage response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        string result = await response.Content.ReadAsStringAsync();

        var serializer = new XmlSerializer(typeof(FeedModel.Feed));
        using TextReader reader = new StringReader(result);

        var deserializedFeed = serializer.Deserialize(reader) as FeedModel.Feed;

        // TODO: Error handling and logging
        // TODO: Publish your events
    }
}
