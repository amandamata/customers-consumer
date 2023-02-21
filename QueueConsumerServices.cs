using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using customers_consumer.Messages;
using MediatR;
using Microsoft.Extensions.Options;

namespace customers_consumer;
public class QueueConsumerServices : BackgroundService
{
    private readonly IAmazonSQS _sqs;
    private readonly IOptions<QueueSettings> _queueSettings;
    private readonly IMediator _mediator;
    private readonly ILogger<QueueConsumerServices> _logger;

    public QueueConsumerServices(IAmazonSQS sqs, IOptions<QueueSettings> queueSettings, IMediator mediator, ILogger<QueueConsumerServices> logger)
    {
        _sqs = sqs;
        _queueSettings = queueSettings;
        _mediator = mediator;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queueUrlResponse = await _sqs.GetQueueUrlAsync(_queueSettings.Value.Name, stoppingToken);
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

                var type = Type.GetType($"customers_consumer.Messages.{messageType}");

                if (type is null)
                    _logger.LogWarning("Unknown message type: {MessageType}", messageType);

                var typedMessage = (ISqsMessage)JsonSerializer.Deserialize(x.Body, type!)!;

                try
                {
                    await _mediator.Send(typedMessage, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Message failed");
                }

                await _sqs.DeleteMessageAsync(queueUrlResponse.QueueUrl, x.ReceiptHandle, stoppingToken);

            });
            await Task.Delay(1000, stoppingToken);
        }
    }
}