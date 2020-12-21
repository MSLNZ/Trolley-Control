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
        private bool make_bt_thread = true;
        private bool no_monitor_threads = true;
        private int num_scans = 0;
        byte[] bytes;
        byte[] bytes1;
        byte[] bytes2;

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
            get { return client.Connected; }
            
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
            BlueToothScanThread.IsBackground = true;
            BlueToothScanThread.Start();
        }

        private void scan()
        {
            bool found_trolley = false;
            num_scans++;
            while (!found_trolley)
            {
                items.Clear();
                try
                {
                    if (client != null)
                    {
                        
                        client.Close();
                        client.Dispose();
                    }
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
                    invoke_gui(ProcNameTrolley.BLUETOOTH_CONNECTION, "Starting scan for bluetooth devices", false);
                    devices = client.DiscoverDevicesInRange();
                    
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
          
            invoke_gui(ProcNameTrolley.BLUETOOTH_CONNECTION, "Scan Complete", false);
            invoke_gui(ProcNameTrolley.BLUETOOTH_AVAILIBLE, items, false);
            Thread.Sleep(1000);

            //the tunnel trolley has been discovered so we can now try and connect to it
            //make a thread if there isn't one running already

            if (make_bt_thread)
            {
                BluetoothClientThread = new Thread(new ThreadStart(ConnectSerial));
                BluetoothClientThread.Start();
            }
            else ConnectSerial();
                
            
            
        }

        public void ConnectSerial() {

           
            //let this thread recursively call scan until we get connected
            make_bt_thread = false;
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
                invoke_gui(ProcNameTrolley.BLUETOOTH_CONNECTION, "Attempting to pair with trolley", false);
                Thread.Sleep(100);
                client.Connect(ep);
            }
            catch(Exception e)
            {
                scan();
            }
            connected = true;

            try
            {
                //if (serialstream != null) serialstream.Close();
                if (client.Connected) serialstream = client.GetStream();
                else scan();
            }
            catch (InvalidOperationException)
            {
                scan();
            }

            num_scans--;

            if (client.Connected && (num_scans==0))
            {
                make_bt_thread = true;  //this thread will exit because we have a successfull connection and serial stream,  allow for a new connect serial thread if connection is lost
                monitor = true;         //allow for the connection to be monitored
                while (!no_monitor_threads) ;  //wait here until any previous monitor threads have exited
                BluetoothMonitorThread = new Thread(new ThreadStart(MonitorConnection));  //create a new thread to monitor the connection
                BluetoothMonitorThread.IsBackground = true;
                BluetoothMonitorThread.Start();
                invoke_gui(ProcNameTrolley.BLUETOOTH_CONNECTION, "Connected", false);
            }
            //BluetoothClientThread is now finishing, the only thread running from this point is monitor connection
        }

        public void MonitorConnection()
        {
            no_monitor_threads = false;
            bytes1 = new byte[1];
            bytes1[0] = (byte) 'z';

            
            

            while (monitor)
            {

                serialstream.ReadTimeout = 1000;
                try
                {
                    Thread.Sleep(3000);
                    serialstream.BeginWrite(bytes1, 0, bytes1.Length, new AsyncCallback(MonitorWriteComplete), serialstream);
                    serialstream.Write(bytes1, 0, bytes1.Length);
                    
                }
                catch (IOException)
                {
                    invoke_gui(ProcNameTrolley.BLUETOOTH_CONNECTION, "Connection lost attempting to reestablish./n Try power cycling the trolley", true);
                    connected = false;
                    monitor = false;
                    StartScan();
                    break;
                }

                catch (TimeoutException)
                {
                    invoke_gui(ProcNameTrolley.BLUETOOTH_CONNECTION, "Connection lost attempting to reestablish./n Try power cycling the trolley", true);
                    connected = false;
                    monitor = false;
                    StartScan();
                    break;
                }
                catch (ObjectDisposedException)
                {
                    invoke_gui(ProcNameTrolley.BLUETOOTH_CONNECTION, "Connection lost attempting to reestablish./n Try power cycling the trolley", true);
                    connected = false;
                    monitor = false;
                    StartScan();
                    break;
                }

            }
            no_monitor_threads = true;
        }

        public bool sendData(byte[] command, int offset, int count)
        {
            bytes = new byte[count];
           
            
                if (client.Connected)
                {

                    try
                    {
                        while (!serialstream.CanWrite) ;
                        serialstream.Flush();
                        //serialstream.BeginWrite(command, offset, count, new AsyncCallback(WriteComplete), serialstream);
                        serialstream.Write(command, offset, count);
                        //serialstream.ReadTimeout = 1000;
                        //Thread.Sleep(10);
                        

                    return true;
                    }
                    catch (IOException)
                    {
                        
                        return false;
                    }
                    catch (TimeoutException)
                    {
                        return false;
                    }
                }
            
            return false;
        }
        public void WriteComplete(IAsyncResult t)
        {
            //serialstream.Read(bytes, 0, count);
            invoke_gui(ProcNameTrolley.BLUETOOTH_DATA, Encoding.Default.GetString(bytes), true);
            bytes = new byte[10];
            serialstream.BeginRead(bytes, 0, bytes.Length, new AsyncCallback(ReadComplete), serialstream);
        }
        public void ReadComplete(IAsyncResult t)
        {
            invoke_gui(ProcNameTrolley.BLUETOOTH_DATA, Encoding.Default.GetString(bytes), true);
        }
        public void MonitorWriteComplete(IAsyncResult t)
        {
            invoke_gui(ProcNameTrolley.BLUETOOTH_DATA, "WRITE: " + Encoding.Default.GetString(bytes1), true);

            bytes2 = new byte[1];
            serialstream.BeginRead(bytes2, 0, bytes2.Length, new AsyncCallback(MonitorReadComplete), serialstream);
        }
        public void MonitorReadComplete(IAsyncResult t)
        {
            invoke_gui(ProcNameTrolley.BLUETOOTH_DATA, "READ: " + Encoding.Default.GetString(bytes2), true);
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
