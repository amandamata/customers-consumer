using MediatR;

namespace customers_consumer.Messages;

public class CustomerDeleted : ISqsMessage
{
    public required Guid Id { get; init; }
}
