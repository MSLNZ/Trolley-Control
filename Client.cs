using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Trolley_Control
{
    public sealed class Client : IDisposable
    {
        private TcpClient client;
        private NetworkStream stream;

        private int timeout = 5000;
        private int port = 1000;
        private string ipAddress = string.Empty;

        private readonly object ioLock = new object();

        public string IP
        {
            get => ipAddress;
            set => ipAddress = value ?? string.Empty;
        }

        public int Port
        {
            get => port;
            set
            {
                if (value < 1 || value > 65535)
                    throw new ArgumentOutOfRangeException(
                        nameof(value),
                        "Port must be between 1 and 65535.");

                port = value;
            }
        }

        public int Timeout
        {
            get => timeout;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(
                        nameof(value),
                        "Timeout must be greater than zero.");

                timeout = value;
            }
        }

        public bool Connect()
        {
            return Connect(IP, Port);
        }

        public bool Connect(string server, int serverPort)
        {
            if (string.IsNullOrWhiteSpace(server))
                throw new ArgumentException(
                    "A server name or IP address is required.",
                    nameof(server));

            CloseConnection();

            try
            {
                client = new TcpClient
                {
                    SendTimeout = timeout,
                    ReceiveTimeout = timeout,
                    NoDelay = true
                };

                // TcpClient.Connect(string, int) handles hostname resolution
                // and avoids relying on DNS address ordering.
                client.Connect(server, serverPort);

                stream = client.GetStream();
                stream.ReadTimeout = timeout;
                stream.WriteTimeout = timeout;

                IP = server;
                Port = serverPort;

                return true;
            }
            catch (SocketException)
            {
                CloseConnection();
                return false;
            }
            catch (IOException)
            {
                CloseConnection();
                return false;
            }
        }

        public bool IsConnected()
        {
            if (client == null || stream == null || !client.Connected)
                return false;

            try
            {
                Socket socket = client.Client;

                // If readable and no bytes are available, the remote side
                // has performed an orderly shutdown.
                bool disconnected =
                    socket.Poll(0, SelectMode.SelectRead) &&
                    socket.Available == 0;

                return !disconnected;
            }
            catch (SocketException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        public bool SendData(string command)
        {
            try
            {
                lock (ioLock)
                {
                    EnsureConnected();
                    WriteCommand(command);
                    return true;
                }
            }
            catch (IOException)
            {
                CloseConnection();
                return false;
            }
            catch (SocketException)
            {
                CloseConnection();
                return false;
            }
            catch (ObjectDisposedException)
            {
                CloseConnection();
                return false;
            }
        }

        public bool SendReceiveData(
            string command,
            ref string result)
        {
            result = string.Empty;

            try
            {
                lock (ioLock)
                {
                    EnsureConnected();
                    WriteCommand(command);
                    result = ReadToCarriageReturn();
                    return true;
                }
            }
            catch (IOException)
            {
                CloseConnection();
                return false;
            }
            catch (SocketException)
            {
                CloseConnection();
                return false;
            }
            catch (ObjectDisposedException)
            {
                CloseConnection();
                return false;
            }
        }

        private void WriteCommand(string command)
        {
            if (stream == null)
                throw new InvalidOperationException(
                    "The client is not connected.");

            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentException(
                    "The command cannot be empty.",
                    nameof(command));

            // The milliK manual specifies CR, ASCII 13, as the terminator.
            string terminatedCommand =
                command.TrimEnd('\r', '\n') + "\r";

            byte[] data =
                Encoding.ASCII.GetBytes(terminatedCommand);

            stream.Write(data, 0, data.Length);
            stream.Flush();
        }

        private string ReadToCarriageReturn()
        {
            if (stream == null)
                throw new InvalidOperationException(
                    "The client is not connected.");

            var response = new StringBuilder();
            var buffer = new byte[1];

            while (true)
            {
                int count = stream.Read(buffer, 0, 1);

                if (count == 0)
                {
                    throw new IOException(
                        "The remote instrument closed the connection.");
                }

                char character = (char)buffer[0];

                if (character == '\r')
                    break;

                // Tolerate CRLF even though the milliK documents CR.
                if (character != '\n')
                    response.Append(character);
            }

            return response.ToString().Trim();
        }

        private void EnsureConnected()
        {
            if (!IsConnected())
            {
                throw new InvalidOperationException(
                    "The TCP client is not connected.");
            }
        }

        public bool CloseConnection()
        {
            bool success = true;

            try
            {
                stream?.Close();
            }
            catch
            {
                success = false;
            }
            finally
            {
                stream = null;
            }

            try
            {
                client?.Close();
            }
            catch
            {
                success = false;
            }
            finally
            {
                client = null;
            }

            return success;
        }

        // Compatibility with the original method name.
        public bool closeConnection()
        {
            return CloseConnection();
        }

        // Compatibility with the original method name.
        public bool isConnected()
        {
            return IsConnected();
        }

        // Compatibility with the original method name.
        public bool sendReceiveData(
            string request,
            ref string result)
        {
            return SendReceiveData(request, ref result);
        }

        public void Dispose()
        {
            CloseConnection();
        }
    }
}