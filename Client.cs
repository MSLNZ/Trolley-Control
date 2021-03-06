﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Net.NetworkInformation;
//using System.Net.Http;
//using System.Net.Http.Headers;




namespace Trolley_Control
{
    public class Client
    {
        private TcpClient client;
        private NetworkStream stream;
        private int timeout = 2000; //The default timeout

        public Client()
        {

        }

        public int Timeout
        {
            get { return timeout; }
            set { timeout = value; }
        }

        public bool Connect(String server, int port)
        {
            try
            {
                //client = new HttpClient();
                //client.BaseAddress = new Uri("http://" + server + ":2000/");
                //client.DefaultRequestHeaders.Accept.Clear();
                //client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("-r -S "));
                // Create a TcpClient. 
                // Note, for this client to work you need to have a TcpServer  
                // connected to the same address as specified by the server, port 
                // combination.
                // Connect to the specified host.
                if (client != null)
                {
                    client.Close(); //allows thread associated with previous socket connection to terminate. 
                }

                client = new TcpClient();

                //get IP addresses. 1st address is ip6, 2nd is ip4
                //if there is only one address returned then it is ip4


                IPAddress ip4;

                IPAddress[] IPAddresses = Dns.GetHostAddresses(server);


                if (IPAddresses.Length == 2)
                {
                    ip4 = IPAddresses[0];
                }
                else ip4 = IPAddresses[0];

                client.Connect(ip4, port);



                return client.Connected;

            }

            catch (SocketException e)
            {
                return false;
            }
        }

        public bool isConnected()
        {
            if (client == null) return false;
            try
            {
                return client.Connected;
            }
            catch (SocketException)
            {
                return false;
            }


        }

        public bool sendReceiveData(String request, ref string result)
        {
            try
            {
                // Translate the passed message into ASCII and store it as a Byte array.
                Byte[] data = System.Text.Encoding.ASCII.GetBytes(request);

                // Get a client stream for reading and writing. 
                //  Stream stream = client.GetStream();
                client.SendTimeout = timeout;
                stream = client.GetStream();

                // Send the message to the connected TcpServer. 
                stream.Write(data, 0, data.Length);

                //Console.WriteLine("Sent: {0}", request);

                // Receive the TcpServer.response. 

                // Buffer to store the response bytes.
                data = new Byte[256];



                stream.ReadTimeout = timeout;
                //stream.BeginRead(data,0,data.Length,)
                // Read the first batch of the TcpServer response bytes.
                Int32 bytes = stream.Read(data, 0, data.Length);
                result = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                //Console.WriteLine("Received: {0}", responseData);


                return true;
            }
            catch (ArgumentNullException)
            {
                return false;
            }
            catch (System.IO.IOException e)
            {
                //This error usually occurs because the device failed to respond.  
                //The appropriate course of action here is to close the Network stream and Close the socket connection (this releases resoures appropriately).
                if (client != null) client.Close(); // this will close the network stream associated with the socket connection too.
                return false;
            }
            catch (TimeoutException)
            {
                return false;
            }

        }
        public bool closeConnection()
        {
            try
            {
                // Close everything.
                stream.Close();
                client.Close();
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
        }

    }
}