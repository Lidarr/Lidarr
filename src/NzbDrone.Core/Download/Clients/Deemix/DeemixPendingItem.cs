using System;
using System.Threading;

namespace NzbDrone.Core.Download.Clients.Deemix
{
    public class DeemixPendingItem<T> : IDisposable
    {
        private readonly ManualResetEventSlim _ack = new ManualResetEventSlim(false);
        private bool _disposed;

        public T Item { get; set; }

        public void Ack()
        {
            _ack.Set();
        }

        public bool Wait(int timeout) => _ack.Wait(timeout);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                // Dispose managed state (managed objects).
                _ack.Dispose();
            }

            _disposed = true;
        }
    }
}
