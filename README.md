# LatestFilingsEventPublisher

* This project is useful for monitoring the latest filings in near real time and potentially alerting or triggering other processes. 
It is not for collecting and storing historical data. For that, you can explore other options from the SEC website. 

* Triggered by [EventBridge Scheduler](https://docs.aws.amazon.com/scheduler/latest/UserGuide/setting-up.html) on any frequency > 10 seconds between 6 a.m. and 10 p.m. EST.
* Polls the SEC website for the latest filings and publishes them to an SNS topic.
* At the time of writing this, the SEC websites recommends polling the RSS feed for the latest filings to get as close to real time updates as possible. Read the docs here: https://www.sec.gov/os/webmaster-faq.

One notable point:

    Sample Declared Bot Request Headers:

        User-Agent: Sample Company Name AdminContact@<sample company domain>.com

        Accept-Encoding: gzip, deflate

        Host: www.sec.gov

However, if you do not provide the User-Agent in a format such as: `Mozilla/5.0 (YourCompany you@example.com)`, you will receive a 403 Forbidden response.

## Here are some steps to follow from Visual Studio:

To deploy your function to AWS Lambda, right click the project in Solution Explorer and select *Publish to AWS Lambda*.

To view your deployed function open its Function View window by double-clicking the function name shown beneath the AWS Lambda node in the AWS Explorer tree.

To perform testing against your deployed function use the Test Invoke tab in the opened Function View window.

To configure event sources for your deployed function, for example to have your function invoked when an object is created in an Amazon S3 bucket, use the Event Sources tab in the opened Function View window.

To update the runtime configuration of your deployed function use the Configuration tab in the opened Function View window.

To view execution logs of invocations of your function use the Logs tab in the opened Function View window.

## Here are some steps to follow to get started from the command line:

Once you have edited your template and code you can deploy your application using the [Amazon.Lambda.Tools Global Tool](https://github.com/aws/aws-extensions-for-dotnet-cli#aws-lambda-amazonlambdatools) from the command line.  Version 5.6.0
or later is required to deploy this project.

Install Amazon.Lambda.Tools Global Tools if not already installed.
```
    dotnet tool install -g Amazon.Lambda.Tools
```

If already installed check if new version is available.
```
    dotnet tool update -g Amazon.Lambda.Tools
```

Execute unit tests
```
    cd "LatestFilingsEventPublisher/test/LatestFilingsEventPublisher.Tests"
    dotnet test
```

Deploy function to AWS Lambda
```
    cd "LatestFilingsEventPublisher/src/LatestFilingsEventPublisher"
    dotnet lambda deploy-function
```
