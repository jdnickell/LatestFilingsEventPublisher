using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using System.Globalization;
using System.Runtime.InteropServices;

namespace LatestFilingsEventPublisher;

/// <inheritdoc />
public class LastRunTimestampManager : ILambdaRunTimestampManager
{
    const string PARAMETER_NAME = "LatestFilingsEventPublisher-LastRunTimestamp";

    /// <inheritdoc />
    public async Task<DateTime?> GetLastRunTimestampEasternAsync()
    {
        using var client = new AmazonSimpleSystemsManagementClient();
        var request = new GetParameterRequest
        {
            Name = PARAMETER_NAME
        };
        var response = await client.GetParameterAsync(request);

        if (response.Parameter == null || response.Parameter.Value == null)
        {
            return null;
        }

        var lastRunTimeStampUtc = DateTime.Parse(response.Parameter.Value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);

        return GetEasternTime(lastRunTimeStampUtc);
    }

    /// <inheritdoc />
    public async Task SetLastRunTimestampAsync(DateTime timestamp)
    {
        using var client = new AmazonSimpleSystemsManagementClient();
        var request = new PutParameterRequest
        {
            Name = PARAMETER_NAME,
            Value = GetEasternTime(timestamp).ToString("o"),
            Type = ParameterType.String,
            Overwrite = true
        };
        var response = await client.PutParameterAsync(request);
        if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
        {
            throw new Exception("Failed to set last run timestamp.");
        }
    }

    /// <summary>
    /// SEC datetimes are in Eastern time. The AWS Lambda .NET 8 runtime is built on the Amazon Linux 2023 (AL2023) minimal container image.
    /// Converts the UTC time to Eastern time for Windows or Linux depending on the host.
    /// </summary>
    /// <returns></returns>
    private DateTime GetEasternTime(DateTime dateTime)
    {
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var timeZoneId = isWindows ? "Eastern Standard Time" : "America/New_York";

        TimeZoneInfo easternZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        DateTime easternTime = TimeZoneInfo.ConvertTimeFromUtc(dateTime, easternZone); // automatically takes into account daylight saving time.
        return easternTime;
    }
}

/// <summary>
/// Manager for the last run timestamp of the Lambda function getting and setting the value in Parameter Store.
/// This is just a quick and easy option if you don't have a database or other storage mechanism available.
/// </summary>
public interface ILambdaRunTimestampManager
{
    /// <summary>
    /// Gets the last run timestamp of the Lambda function from Parameter Store and converts it to eastern time.
    /// </summary>
    /// <returns>DateTime of the last run timestamp in eastern.</returns>
    /// <exception cref="Exception"></exception>
    Task<DateTime?> GetLastRunTimestampEasternAsync();

    /// <summary>
    /// Sets the last run timestamp (in utc) of the Lambda function in Parameter Store.
    /// </summary>
    /// <param name="timestamp"></param>
    /// <returns>An awaitable Task.</returns>
    /// <exception cref="Exception"></exception>
    Task SetLastRunTimestampAsync(DateTime timestamp);
}
