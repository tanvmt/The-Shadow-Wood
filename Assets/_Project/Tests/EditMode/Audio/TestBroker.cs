using System;
using System.Collections.Generic;
using MessagePipe;

namespace TheShadowWood.Audio.Tests
{
    // Synchronous in-memory broker that also records what was published.
    internal sealed class TestBroker<T> : IPublisher<T>, ISubscriber<T>
    {
        private readonly List<IMessageHandler<T>> _handlers = new List<IMessageHandler<T>>();

        public List<T> Published { get; } = new List<T>();
        public int SubscriberCount => _handlers.Count;

        public void Publish(T message)
        {
            Published.Add(message);
            foreach (IMessageHandler<T> handler in _handlers.ToArray())
            {
                handler.Handle(message);
            }
        }

        public IDisposable Subscribe(IMessageHandler<T> handler, params MessageHandlerFilter<T>[] filters)
        {
            _handlers.Add(handler);
            return new Subscription(() => _handlers.Remove(handler));
        }

        private sealed class Subscription : IDisposable
        {
            private Action _dispose;

            public Subscription(Action dispose)
            {
                _dispose = dispose;
            }

            public void Dispose()
            {
                _dispose?.Invoke();
                _dispose = null;
            }
        }
    }
}
