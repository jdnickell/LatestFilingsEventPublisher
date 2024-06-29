using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;

namespace LatestFilingsEventPublisher;

/// <inheritdoc />
public class LastRunTimestampManager(IAmazonSimpleSystemsManagement ssmClient) : ILambdaRunTimestampManager
{
    const string PARAMETER_NAME = "LatestFilingsEventPublisher-LastRunTimestamp";
    private readonly IAmazonSimpleSystemsManagement _ssmClient = ssmClient;

    /// <inheritdoc />
    public async Task<DateTimeOffset> GetLastRunTimestampAsync()
    {
        var request = new GetParameterRequest
        {
            Name = PARAMETER_NAME
        };
        var response = await _ssmClient.GetParameterAsync(request);

        if (response.Parameter == null || response.Parameter.Value == null)
        {
            return DateTimeOffset.UtcNow;
        }

        var lastRunTimeStamp = DateTimeOffset.Parse(response.Parameter.Value);
        return lastRunTimeStamp;
    }

    /// <inheritdoc />
    public async Task SetLastRunTimestampAsync(DateTimeOffset timestamp)
    {
        var request = new PutParameterRequest
        {
            Name = PARAMETER_NAME,
            Value = timestamp.ToString(),
            Type = ParameterType.String,
            Overwrite = true
        };
        var response = await _ssmClient.PutParameterAsync(request);
        if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
        {
            throw new Exception("Failed to set last run timestamp.");
        }
    }
}

/// <summary>
/// Manager for the last run timestamp of the Lambda function getting and setting the value in Parameter Store.
/// This is just a quick and easy option if you don't have a database or other storage mechanism available.
/// </summary>
public interface ILambdaRunTimestampManager
{
    /// <summary>
    /// Gets the last run timestamp of the Lambda function from Parameter Store. If it's null, it will return the current timestamp.
    /// </summary>
    /// <returns>DateTime of the last run timestamp.</returns>
    /// <exception cref="Exception"></exception>
    Task<DateTimeOffset> GetLastRunTimestampAsync();

    /// <summary>
    /// Sets the last run timestamp of the Lambda function in Parameter Store.
    /// </summary>
    /// <param name="timestamp"></param>
    /// <returns>An awaitable Task.</returns>
    /// <exception cref="Exception"></exception>
    Task SetLastRunTimestampAsync(DateTimeOffset timestamp);
}
