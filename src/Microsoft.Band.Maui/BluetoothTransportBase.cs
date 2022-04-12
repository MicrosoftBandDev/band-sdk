// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Store.BluetoothTransportBase
// Assembly: Microsoft.Band.Store, Version=1.3.20628.2, Culture=neutral, PublicKeyToken=608d7da3159f502b
// MVID: 91750BE8-70C6-4542-841C-664EE611AF0B
// Assembly location: .\netcore451\Microsoft.Band.Store.dll

using System;

namespace Microsoft.Band
{
    internal abstract partial class BluetoothTransportBase : IDisposable
    {
        private bool isConnected;
        private ICargoStream cargoStream;
        private CargoStreamReader cargoReader;
        private bool disposed;
        protected ILoggerProvider loggerProvider;

        public event EventHandler<TransportDisconnectedEventArgs> Disconnected;

        protected BluetoothTransportBase(ILoggerProvider loggerProvider)
        {
            this.loggerProvider = loggerProvider;
            isConnected = false;
        }

        public ICargoStream CargoStream
        {
            get
            {
                CheckIfDisposed();
                return cargoStream;
            }
        }

        public CargoStreamReader CargoReader
        {
            get
            {
                CheckIfDisposed();
                return cargoReader;
            }
        }

        public BandConnectionType ConnectionType => BandConnectionType.Bluetooth;

        protected void RaiseDisconnectedEvent(TransportDisconnectedEventArgs args)
        {
            EventHandler<TransportDisconnectedEventArgs> disconnected = Disconnected;
            if (disconnected == null)
                return;
            disconnected(this, args);
        }

        public virtual void Disconnect()
        {
            if (IsConnected)
            {
                isConnected = false;
                CargoStreamReader cargoReader = this.cargoReader;
                if (cargoReader != null)
                {
                    cargoReader.Dispose();
                    this.cargoReader = null;
                }
                cargoStream = null;
            }
            RaiseDisconnectedEvent(new TransportDisconnectedEventArgs(TransportDisconnectedReason.DisconnectCall));
        }

        public bool IsConnected => isConnected;

        protected void CheckIfDisconnected()
        {
            if (!IsConnected)
                throw new InvalidOperationException("Transport not connected");
        }

        protected void CheckIfDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(BluetoothTransportBase));
        }

        public void Dispose() => Dispose(true);

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing || disposed)
                return;
            Disconnect();
            ICargoStream cargoStream = this.cargoStream;
            if (cargoStream != null)
            {
                cargoStream.Dispose();
                this.cargoStream = null;
            }
            disposed = true;
        }
    }
}
