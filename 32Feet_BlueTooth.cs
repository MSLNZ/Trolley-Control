using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using InTheHand;
using InTheHand.Net;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Ports;
using InTheHand.Net.Sockets;
using System.IO;
using System.Net.Sockets;
using System.IO.Ports;

namespace Trolley_Control
{
    class _32Feet_BlueTooth
    {
        private Thread BlueToothScanThread;
        private Thread BluetoothClientThread;
        private Thread BluetoothMonitorThread;

        private bool connected = false;
        private NetworkStream serialstream;
        private Guid nguid;
        private TrolleyUpdateGUI invoke_gui;
        private BluetoothDeviceInfo[] devices;
        private int tunnel_device_index;
        private BluetoothClient client;
        private List<string> items;
        private bool monitor = true;

        private const string address = "00126F2082F4";


        public _32Feet_BlueTooth(TrolleyUpdateGUI gui_updater)
        {
            invoke_gui = gui_updater;
            nguid = new Guid("00001101-0000-1000-8000-00805F9B34FB");
            items = new List<string>();


            StartScan();

        }

        public bool Connected
        {
            get { return connected; }
            set { connected = value; }
        }

        public void ConnectAsServer()
        {
            //BluetoothClientThread = new Thread(new ThreadStart(ClientConnectThreadStart));
        }

        public void ClientConnectThreadStart()
        {
            
        }
        public void ConnectAsClient()
        {
            
        }

        public void StartScan()
        {
            
            BlueToothScanThread = new Thread(new ThreadStart(scan));
            BlueToothScanThread.Start();
        }

        private void scan()
        {
            while (true)
            {
                items.Clear();
                try
                {
                    client = new BluetoothClient();
               
                }
                catch (PlatformNotSupportedException)
                {
                    //invoke_gui(ProcNameTrolley.BLUETOOTH_CONNECTION, "Bluetooth Dongle not found - plug in Dongle and restart this program", true);
                    //Thread.Sleep(2000);
                }

                if (client != null)
                {
                
                    
                    Thread.Sleep(1000);
                    //invoke_gui(ProcNameTrolley.BLUETOOTH_CONNECTION, "Starting scan for bluetooth devices", false);
                    devices = client.DiscoverDevicesInRange();
                    bool found_trolley = false;
                    int index = 0;

                    foreach (BluetoothDeviceInfo d in devices)
                    {
                        items.Add(d.DeviceName);
                        if (d.DeviceAddress.ToString() == address)
                        {
                            
                            found_trolley = true;
                            tunnel_device_index = index;
                        }
                        index++;
                    }
                    if (found_trolley) break;



                }
            }
          
            //invoke_gui(ProcNameTrolley.BLUETOOTH_CONNECTION, "Scan Complete", false);
            //invoke_gui(ProcNameTrolley.BLUETOOTH_AVAILIBLE, items, false);


            //the tunnel trolley has been discovered so we can now try and connect to it
            BluetoothClientThread = new Thread(new ThreadStart(ConnectSerial));
            BluetoothClientThread.Start();
            //invoke_gui(ProcNameTrolley.BLUETOOTH_CONNECTION, "Attempting to pair with trolley", false);
            
        }

        public void ConnectSerial() {

            //this is the address we want  
            BluetoothAddress addr = BluetoothAddress.Parse(address);
            nguid = new Guid("00001101-0000-1000-8000-00805F9B34FB");
            BluetoothEndPoint ep = new BluetoothEndPoint(addr, nguid);
            //client.SetPin("1234");
            if (client.Available == 1)
            {

                client.Authenticate = false;
            }
           
            //client.LingerState = new LingerOption(true,60);

            try
            {
                client.Connect(ep);
            }
            catch(Exception e)
            {
                scan();
            }
            connected = true;

            try
            {
                if(client.Connected) serialstream = client.GetStream();
                
                
            }
            catch (InvalidOperationException)
            {
                scan();
            }


            if (client.Connected)
            {
                BluetoothMonitorThread = new Thread(new ThreadStart(MonitorConnection));
                BluetoothMonitorThread.Start();
                invoke_gui(ProcNameTrolley.BLUETOOTH_CONNECTION, "Connected", false);
            }
            
        }

        public void MonitorConnection()
        {
            byte[] bytes = new byte[1];
            bytes[0] = (byte) 'z';

            byte[] bytes1 = new byte[1];
            bytes1[0]= (byte) '1';
            
            while (true)
            {
                if (monitor)
                {
                    serialstream.ReadTimeout = 100;
                    try
                    {
                        Thread.Sleep(3000);
                        serialstream.Write(bytes, 0, 1);
                        serialstream.Read(bytes1, 0, 1);
                    }
                    catch (IOException)
                    {
                        //invoke_gui(ProcNameTrolley.BLUETOOTH_CONNECTION, "Connection lost attempting to reestablish./n Try power cycling the trolley", true);
                        connected = false;
                        StartScan();

                        break;
                    }

                    catch (TimeoutException)
                    {
                        //invoke_gui(ProcNameTrolley.BLUETOOTH_CONNECTION, "Connection lost attempting to reestablish./n Try power cycling the trolley", true);
                        connected = false;
                        StartScan();
                    }
                }
            }
        }

        public bool sendData(byte[] command, int offset, int count)
        {
            byte[] bytes = new byte[1];
            
                if (client.Connected)
                {
                    monitor = false;

                    try
                    {
                        while (!serialstream.CanWrite) ;
                        serialstream.Flush();
                        serialstream.Write(command, offset, count);
                        serialstream.ReadTimeout = 1000;
                        //Thread.Sleep(50);
                        serialstream.Read(bytes, 0, 1);

                        monitor = true;
                        return true;
                    }
                    catch (IOException)
                    {
                        monitor = true;
                        return false;
                    }
                    catch (TimeoutException)
                    {
                        return false;
                    }
                }
            
            return false;
        }
        public bool Writebyte(byte command)
        {
            byte[] writebyte = new byte[1];
            byte[] readbyte = new byte[1];
            readbyte[0] = 0;
            writebyte[0] = command;
            byte[] bytes = new byte[10];
            if (client.Connected)
            {
                monitor = false;

                try
                {  
                    int i = 0;
                    while (i < 20)
                    {
                       
                        while (!serialstream.CanWrite) ;
                        serialstream.Flush();
                        serialstream.Write(writebyte, 0, 1);
                        serialstream.ReadTimeout = 1000;
                        Thread.Sleep(50);
                        serialstream.Read(readbyte, 0, 1);

                        if (readbyte[0] == writebyte[0])
                        {
                            monitor = true;
                            return true;
                        }

                        i++;
                    }
                    return false;
                }
                catch (IOException)
                {
                    monitor = true;
                    return false;
                }
                catch (TimeoutException)
                {
                    monitor = true;
                    return false;
                }
            }
            return false;
        }
        

       
        
    }
}
