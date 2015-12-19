using System;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Threading;
using System.Net.Sockets;
using System.Net;

namespace nboard
{
    class TcpServer
    {
        private TcpListener _server;
        private Boolean _isRunning;

        public event Action<HttpConnection> ConnectionAdded;

        public TcpServer(int port)
        {
            _server = new TcpListener(IPAddress.Any, port);
        }

        public void Run()
        {
            _server.Start();
            _isRunning = true;
            LoopClients();
        }

        public void Stop()
        {
            _isRunning = false;
        }

        private void LoopClients()
        {
            while (_isRunning)
            {
                if (!_server.Pending())
                {
                    Thread.Sleep(100);
                    continue;
                }

                TcpClient newClient = _server.AcceptTcpClient();
                Thread t = new Thread(new ParameterizedThreadStart(HandleClient));
                t.Start(newClient);
            }
        }

        private void HandleClient(object obj)
        {
            TcpClient client = (TcpClient)obj;
            StreamWriter writer = new StreamWriter(client.GetStream(), Encoding.ASCII);
            var reader = client.GetStream();
            String readData = "";
            reader.ReadTimeout = 100;
            var buffer = new byte[1024];
            int len = 0;

            try
            {
                do
                {
                    len = reader.Read(buffer, 0, buffer.Length);
                    var block = System.Text.Encoding.UTF8.GetString(buffer, 0, len);
                    readData += block;
                }
                while (len > 0);
            }

            catch (IOException)
            {
                // that's ok, we've reached end of the stream (read timeout)
            }

            if (ConnectionAdded != null)
            {
                ConnectionAdded(new HttpConnection(readData, response =>
                {
                    writer.Write(response);
                    writer.Flush();
                    client.Close();
                }));
            }
        }
    }
}