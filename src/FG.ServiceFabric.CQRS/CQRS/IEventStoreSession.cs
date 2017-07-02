﻿using System;
using System.Threading.Tasks;

namespace FG.ServiceFabric.CQRS
{
    public interface IEventStoreSession
    {
        Task<TAggregateRoot> Get<TAggregateRoot>()
            where TAggregateRoot : class, IEventStored, new();
        Task SaveChanges();
        Task Delete();
    }
}
