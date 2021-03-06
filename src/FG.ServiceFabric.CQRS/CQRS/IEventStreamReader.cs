using System;
using System.Threading;
using System.Threading.Tasks;

namespace FG.ServiceFabric.CQRS
{
    public interface IEventStreamReader<TEventStream> where TEventStream : IDomainEventStream
    {
        Task<TEventStream> GetEventStreamAsync(Guid id, CancellationToken cancellationToken);
    }
}