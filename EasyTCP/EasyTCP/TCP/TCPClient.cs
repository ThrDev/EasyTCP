﻿using EasyTCP.TCP.Events;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace EasyTCP.TCP
{
    public class TCPClient
    {
        public event EventHandler<ClientArgs> Connected = delegate { };
        public event EventHandler<DataArgs> Received = delegate { };
        public event EventHandler<ClientArgs> Disconnected = delegate { };
        //public EventHandler<DataArgs> Sent = delegate { };
        public Socket sock;
        byte[] buffer = new byte[512];
        //create an entire TCP client.
        public TCPClient()
        {
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //setup client, timeouts, etc.
        }

        public void Connect(IPAddress IP, int Port)
        {
            sock.BeginConnect(IP, Port, new AsyncCallback(this.OnConnected), sock);
        }

        public TCPClient(Socket client)
        {
            sock = client;
            sock.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(this.OnReceived), sock);
        }

        public void Send(byte[] buffer, int offset = 0, int count = 0)
        {
            sock.BeginSend(buffer, offset, (count > 0 ? count : buffer.Length), SocketFlags.None, new AsyncCallback(OnSent), sock);
        }

        private void OnSent(IAsyncResult res)
        {
            int sent = ((Socket)res.AsyncState).EndSend(res);
            if (sent <= 0)
            {
                Disconnect();
            }
        }

        public void Disconnect()
        {
            try
            {
                sock.Close(10);
            }
            catch
            {
                //
            }
            Disconnected(this, new ClientArgs(this));
            //stop everything.
        }
        private void OnConnected(IAsyncResult res)
        {
            ((Socket)res.AsyncState).EndConnect(res);
            //callback the on connected delegate.
            sock.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(this.OnReceived), sock);
            Connected(this, new ClientArgs(this));
        }
        private void OnReceived(IAsyncResult res)
        {
            Socket x = (Socket)res.AsyncState;
            try
            {
                int received = x.EndReceive(res);
                //check if disconnected.
                if (received <= 0)
                {
                    //disconnected.
                    try
                    {
                        this.Disconnected(this, new ClientArgs(this));
                        x.Close();
                        this.buffer = null;
                        return;
                    }
                    catch { }
                }
                //throw receieved event.
                this.Received(this, new DataArgs(this, buffer, 0, received));
                try
                {
                    x.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(OnReceived), x);
                }
                catch { 
                //client disconnected.
                }
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }
    }
}
