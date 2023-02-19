using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using customers_consumer.Messages;
using Microsoft.Extensions.Options;

namespace customers_consumer;
public class QueueConsumerServices : BackgroundService
{
    private readonly IAmazonSQS _sqs;
    private readonly IOptions<QueueSettings> _queueSettings;
    public QueueConsumerServices(IAmazonSQS sqs, IOptions<QueueSettings> queueSettings)
    {
        _sqs = sqs;
        _queueSettings = queueSettings;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queueUrlResponse = await _sqs.GetQueueUrlAsync("customers", stoppingToken);
        var receiveMessageRequest = new ReceiveMessageRequest
        {
            QueueUrl = queueUrlResponse.QueueUrl,
            AttributeNames = new List<string> { "All" },
            MessageAttributeNames = new List<string> { "All" },
            MaxNumberOfMessages = 1
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            var response = await _sqs.ReceiveMessageAsync(receiveMessageRequest, stoppingToken);
            response.Messages.ForEach(async x =>
            {
                var messageType = x.MessageAttributes["MessageType"].StringValue;
                switch (messageType)
                {
                    case nameof(CustomerCreated):
                        var created = JsonSerializer.Deserialize<CustomerCreated>(x.Body);
                        break;
                    case nameof(CustomerUpdated):
                        var updated = JsonSerializer.Deserialize<CustomerUpdated>(x.Body);
                        break;
                    case nameof(CustomerDeleted):
                        var deleted = JsonSerializer.Deserialize<CustomerDeleted>(x.Body);
                        break;
                }

                await _sqs.DeleteMessageAsync(queueUrlResponse.QueueUrl, x.ReceiptHandle, stoppingToken);

            });
            await Task.Delay(1000, stoppingToken);
        }
    }
}