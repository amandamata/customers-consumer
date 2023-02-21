using Amazon.SQS;
using customers_consumer;
using MediatR;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<QueueSettings>(builder.Configuration.GetSection(QueueSettings.Key));
builder.Services.AddSingleton<IAmazonSQS, AmazonSQSClient>();
builder.Services.AddHostedService<QueueConsumerServices>();
builder.Services.AddMediatR(typeof(Program));

var app = builder.Build();

app.Run();
