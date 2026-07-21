using System;
using System.Collections.Generic;

namespace Kingdom.Core
{
    public sealed class ServiceRegistry : IDisposable
    {
        private readonly Dictionary<Type, object> services = new();
        private bool disposed;

        public void Register<T>(T service) where T : class
        {
            if (disposed) throw new ObjectDisposedException(nameof(ServiceRegistry));
            services[typeof(T)] = service ?? throw new ArgumentNullException(nameof(service));
        }

        public T Resolve<T>() where T : class
        {
            if (disposed) throw new ObjectDisposedException(nameof(ServiceRegistry));
            if (services.TryGetValue(typeof(T), out object service)) return (T)service;
            throw new InvalidOperationException($"Service {typeof(T).Name} is not registered.");
        }

        public void Dispose()
        {
            if (disposed) return;
            foreach (object service in services.Values)
            {
                if (service is IDisposable disposable) disposable.Dispose();
            }
            services.Clear();
            disposed = true;
        }
    }
}

