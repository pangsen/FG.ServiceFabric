using System;
using System.Collections.Generic;
using FG.ServiceFabric.CQRS.Exceptions;

namespace FG.ServiceFabric.CQRS
{

    public abstract partial class AggregateRoot<TAggregateRootEventInterface> : IAggregateRoot, IEventStored
        where TAggregateRootEventInterface : class, IAggregateRootEvent
    {
        private ITimeProvider _timeProvider;
        private readonly IList<TAggregateRootEventInterface> _uncommittedEvents = new List<TAggregateRootEventInterface>();
        protected int Version;

        public Guid AggregateRootId { get; private set; }
        private void SetId(Guid id) { AggregateRootId = id; }

        protected void RaiseEvent<TDomainEvent>(TDomainEvent aggregateRootEvent) 
            where TDomainEvent : TAggregateRootEventInterface
        {
            if(aggregateRootEvent is IAggregateRootCreatedEvent)
            {
                if (Version != 0)
                {
                    throw new AggregateRootException(
                    $"Expected the event implementing {typeof(IAggregateRootCreatedEvent)} to be first version.");
                }

                if(AggregateRootId != Guid.Empty)
                {
                    throw new AggregateRootException(
                        $"The {nameof(AggregateRootId)} can only be set once. " +
                        $"Only the first event should implement {typeof(IAggregateRootCreatedEvent)}.");
                }

                if(aggregateRootEvent.AggregateRootId == Guid.Empty)
                {
                    throw new AggregateRootException($"Missing {nameof(aggregateRootEvent.AggregateRootId)}");
                }
            }
            else
            {
                if(AggregateRootId == Guid.Empty)
                {
                    throw new AggregateRootException(
                        $"No {nameof(AggregateRootId)} set. " +
                        $"Did you forget to implement {typeof(IAggregateRootCreatedEvent)} in the first event?");
                }

                if(aggregateRootEvent.AggregateRootId != Guid.Empty && aggregateRootEvent.AggregateRootId != AggregateRootId)
                {
                    throw new AggregateRootException(
                        $"{nameof(AggregateRootId)} in event is  {aggregateRootEvent.AggregateRootId} which is different from the current {AggregateRootId}");
                }

                aggregateRootEvent.AggregateRootId = AggregateRootId;
            }

            aggregateRootEvent.Version = Version + 1;
            aggregateRootEvent.UtcTimeStamp = _timeProvider.UtcNow;

            ApplyEvent(aggregateRootEvent);
            AssertInvariantsAreMet();
            _uncommittedEvents.Add(aggregateRootEvent);
            _eventController.StoreDomainEventAsync(aggregateRootEvent);
            _eventController.RaiseDomainEvent(aggregateRootEvent);
        }

        public virtual void AssertInvariantsAreMet()
        {
            if (AggregateRootId == Guid.Empty)
            {
                throw new InvariantsNotMetException($"{nameof(AggregateRootId)} not set.");
            }
        }
        
        private void ApplyEvent(TAggregateRootEventInterface aggregateRootEvent)
        {
            if (aggregateRootEvent is IAggregateRootCreatedEvent)
            {
                SetId(aggregateRootEvent.AggregateRootId);
            }
            Version = aggregateRootEvent.Version;
            _eventDispatcher.Dispatch(aggregateRootEvent);
        }

        private readonly DomainEventDispatcher<TAggregateRootEventInterface> _eventDispatcher = new DomainEventDispatcher<TAggregateRootEventInterface>();
        
        protected DomainEventDispatcher<TAggregateRootEventInterface>.RegistrationBuilder RegisterEventAppliers()
        {
            return _eventDispatcher.RegisterHandlers();
        }

        #region IEventStored

        private IDomainEventController _eventController;
        
        public void Initialize(IDomainEventController eventController, IDomainEvent[] eventStream, ITimeProvider timeProvider = null)
        {
            Initialize(eventController, timeProvider);

            if (eventStream == null)
                return;

            foreach (var domainEvent in eventStream)
            {
                ApplyEvent(domainEvent as TAggregateRootEventInterface);
            }
        }

        public void Initialize(IDomainEventController eventController, ITimeProvider timeProvider = null)
        {
            _timeProvider = timeProvider ?? UtcNowTimeProvider.Instance;
            _eventController = eventController;
        }

        public IEnumerable<IDomainEvent> GetChanges()
        {
            return _uncommittedEvents;
        }

        public void ClearChanges()
        {
            _uncommittedEvents.Clear();
        }

        #endregion
    }
}