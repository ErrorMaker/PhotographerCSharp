﻿using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Photographer.Network.Protocol;

namespace Photographer.Network
{
    public class HConnection : IHConnection, IDisposable
    {
        private int _inSteps, _outSteps;
        private readonly object _disposeLock;
        private readonly object _disconnectLock;

        /// <summary>
        /// Occurs when the connection between the game client, and server have been intercepted.
        /// </summary>
        public event EventHandler Connected;
        protected virtual void OnConnected(EventArgs e)
        {
            Connected?.Invoke(this, e);
        }

        /// <summary>
        /// Occurs when either the game client, or server have disconnected.
        /// </summary>
        public event EventHandler Disconnected;
        protected virtual void OnDisconnected(EventArgs e)
        {
            Disconnected?.Invoke(this, e);
        }

        /// <summary>
        /// Occurs when the game client's outgoing data has been intercepted.
        /// </summary>
        public event EventHandler<DataInterceptedEventArgs> DataOutgoing;
        protected virtual void OnDataOutgoing(DataInterceptedEventArgs e)
        {
            DataOutgoing?.Invoke(this, e);
        }

        /// <summary>
        /// Occrus when the server's incoming data has been intercepted.
        /// </summary>
        public event EventHandler<DataInterceptedEventArgs> DataIncoming;
        protected virtual void OnDataIncoming(DataInterceptedEventArgs e)
        {
            DataIncoming?.Invoke(this, e);
        }

        public int SocketSkip { get; set; } = 1;
        public HNode Local { get; private set; }
        public HNode Remote { get; private set; }
        public bool IsConnected { get; private set; }
        public HotelEndPoint RemoteEndPoint => Remote?.EndPoint;

        public HConnection()
        {
            _disposeLock = new object();
            _disconnectLock = new object();
        }

        public void Intercept(IPEndPoint endpoint)
        {
            Intercept(new HotelEndPoint(endpoint));
        }
        public void Intercept(string host, int port)
        {
            Intercept(HotelEndPoint.Parse(host, port));
        }
        public void Intercept(HotelEndPoint endpoint)
        {
            int interceptCount = 0;
            while (!IsConnected)
            {
                try
                {
                    Local = HNode.Accept(endpoint.Port);
                    if (++interceptCount == SocketSkip)
                    {
                        interceptCount = 0;
                        continue;
                    }

                    byte[] buffer = Local.Peek(6);
                    Remote = HNode.ConnectNew(endpoint);
                    if (BigEndian.ToUInt16(buffer, 4) == 4000)
                    {
                        IsConnected = true;
                        OnConnected(EventArgs.Empty);

                        _inSteps = 0;
                        _outSteps = 0;
                        Task.Factory.StartNew(InterceptOutgoing);
                        Task.Factory.StartNew(InterceptIncoming);
                    }
                    else
                    {
                        buffer = Local.Receive(512);
                        Remote.Send(buffer);

                        buffer = Remote.Receive(1024);
                        Local.Send(buffer);
                        continue;
                    }

                }
                finally
                {
                    if (!IsConnected)
                    {
                        Local?.Dispose();
                        Remote?.Dispose();
                    }
                }
            }
        }

        public int SendToServer(byte[] data)
        {
            return Remote.Send(data);
        }
        public int SendToServer(HMessage packet)
        {
            return Remote.SendPacket(packet);
        }

        public int SendToClient(byte[] data)
        {
            return Local.Send(data);
        }
        public int SendToClient(HMessage packet)
        {
            return Local.SendPacket(packet);
        }

        private void InterceptOutgoing()
        {
            HMessage packet = Local.ReceivePacket();
            if (packet != null)
            {
                var args = new DataInterceptedEventArgs(packet, ++_outSteps, true);
                try { OnDataOutgoing(args); }
                catch { args.Restore(); }

                if (!args.IsBlocked && !args.WasRelayed)
                {
                    SendToServer(args.Packet);
                }
                if (!args.HasContinued)
                {
                    Task.Factory.StartNew(InterceptOutgoing);
                }
            }
            else Disconnect();
        }
        private void InterceptIncoming()
        {
            HMessage packet = Remote.ReceivePacket();
            if (packet != null)
            {
                var args = new DataInterceptedEventArgs(packet, ++_inSteps, false);
                try { OnDataIncoming(args); }
                catch { args.Restore(); }

                if (!args.IsBlocked && !args.WasRelayed)
                {
                    SendToClient(args.Packet);
                }
                if (!args.HasContinued)
                {
                    Task.Factory.StartNew(InterceptIncoming);
                }
            }
            else Disconnect();
        }

        public void Disconnect()
        {
            if (Monitor.TryEnter(_disconnectLock))
            {
                try
                {
                    if (Local != null)
                    {
                        Local.Dispose();
                        Local = null;
                    }
                    if (Remote != null)
                    {
                        Remote.Dispose();
                        Remote = null;
                    }
                    if (IsConnected)
                    {
                        IsConnected = false;
                        OnDisconnected(EventArgs.Empty);
                    }
                }
                finally { Monitor.Exit(_disconnectLock); }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Disconnect();
            }
        }
    }
}