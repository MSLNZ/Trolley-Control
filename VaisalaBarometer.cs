using System;
using System.Threading;
using System.IO;
using System.IO.Ports;
using System.Windows.Forms;
using System.Management;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using System.Linq;


using System.Threading.Tasks;



namespace Trolley_Control
{

    public sealed class SerialPortWatcher : IDisposable
    {
        public SerialPortWatcher()
        {
            _taskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
            ComPorts = new ObservableCollection<string>(SerialPort.GetPortNames().OrderBy(s => s));

            WqlEventQuery query = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent");

            _watcher = new ManagementEventWatcher(query);
            _watcher.EventArrived += (sender, eventArgs) => CheckForNewPorts(eventArgs);
            _watcher.Start();
        }

        private void CheckForNewPorts(EventArrivedEventArgs args)
        {
            // do it async so it is performed in the UI thread if this class has been created in the UI thread
            Task.Factory.StartNew(CheckForNewPortsAsync, CancellationToken.None, TaskCreationOptions.None, _taskScheduler);
        }

        private void CheckForNewPortsAsync()
        {
            IEnumerable<string> ports = SerialPort.GetPortNames().OrderBy(s => s);

            foreach (string comPort in ComPorts)
            {
                if (!ports.Contains(comPort))
                {
                    ComPorts.Remove(comPort);
                }
            }

            foreach (var port in ports)
            {
                if (!ComPorts.Contains(port))
                {
                    AddPort(port);
                }
            }
        }

        private void AddPort(string port)
        {
            for (int j = 0; j < ComPorts.Count; j++)
            {
                if (port.CompareTo(ComPorts[j]) < 0)
                {
                    ComPorts.Insert(j, port);
                    break;
                }
            }

        }

        public ObservableCollection<string> ComPorts { get; private set; }

        #region IDisposable Members

        public void Dispose()
        {
            _watcher.Stop();
        }

        #endregion

        private ManagementEventWatcher _watcher;
        private TaskScheduler _taskScheduler;
    }


    public struct ProcNameSerialCom
    {
        public const short CHECKCOMPORTS = 0;
        public const short POLL = 1;
        public const short IDLE = 255;
    }
    public class VaisalaBarometer:SerialCom
    {

        private Thread serialPortThread;
        private SerialPortWatcher watcher;


    

        public VaisalaBarometer(ref VaisalaUpdateGui pbarug)
        {
            update_gui = pbarug;
            error_reported = false;
            //m = m_;

            //thread to control and monitor the barometer
            serialPortThread = new Thread(new ThreadStart(Query));
            //watcher = new SerialPortWatcher();
            serialPortThread.Start();

            current_exe_stage = ProcNameSerialCom.CHECKCOMPORTS;

        }

       
        public override bool CheckComPorts()
        {
            //get the names of all the com ports
            
            
            string[] ports = SerialPort.GetPortNames();

            foreach (string portname in ports)
            {
                
                Thread.Sleep(100);
                if (Init(portname))
                {
                    break;   //this sets the correct serial port and the port is now open for comunication
                }
                
                
            }
            if (!s_port.IsOpen)
            {
                if (!ErrorReported)
                {
                    
                    update_gui(ProcNameSerialCom.CHECKCOMPORTS, "Barometer Error - Could not find a valid serial COM port - Check the barometer is connected and there is a relevant COM port in the Windows Device manager", true);
                    
                }
                Thread.Sleep(5000);
                return false;
            }
            return true;

        }

        
        public override bool Init(string portname)
        {

            try
            {
                //create a new serial port
                s_port = new SerialPort();

                s_port.PortName = portname;

                s_port.BaudRate = 9600;
                s_port.Parity = Parity.Even;
                s_port.DataBits = 7;
                s_port.StopBits = StopBits.One;
                s_port.Handshake = Handshake.None;
                s_port.RtsEnable = false;
                s_port.DtrEnable = false;
                s_port.ReadTimeout = 2000;
                s_port.WriteTimeout = 1000;
                s_port.Open();
                s_port.DiscardInBuffer();

                

                //put the barometer into broadcast mode
                s_port.Write("send\r");
                //Thread.Sleep(800); //leave a bit of time for a response
                Thread.Sleep(800);
                //check what we get back is correct
                
                string line = s_port.ReadLine();
                

                if (line.Contains("hPa"))
                {
                    is_open = true;
                    return true;
                }

                else
                {
                    //s_port.Close();
                    return false;
                }


            }
            catch (IOException)
            {
                
                s_port.Close();
                is_open = false;
                return false;
            }
            catch (TimeoutException)
            {
                s_port.Close();
                is_open = false;
                return false;
            }
            catch (AccessViolationException)
            {
                
                update_gui(ProcNameSerialCom.CHECKCOMPORTS, "Barometer Error - Serial Port Already Open", !error_reported);
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                
                update_gui(ProcNameSerialCom.CHECKCOMPORTS, "Barometer Error - Serial Port Already Open", !error_reported);
                return false;
            }
        }


        public override bool Read()
        {
            //read if the port is open
            if (s_port.IsOpen)
            {
                s_port.WriteLine("send\r");
                Thread.Sleep(500);
                try
                {
                    string line = s_port.ReadLine();
                    ParseForResult(line);
                
                    return true;

                }
                catch (TimeoutException)
                {
                    //presumably we have lost comunication
                    is_open = false;
                    s_port.Close();
                    s_port.Dispose();
                    Thread.Sleep(10000);
                    current_exe_stage = ProcNameSerialCom.CHECKCOMPORTS;
                    return false;
                }
                catch (System.InvalidOperationException)
                {
                    is_open = false;
                    s_port.Close();
                    s_port.Dispose();
                    Thread.Sleep(1000);
                    current_exe_stage = ProcNameSerialCom.CHECKCOMPORTS;
                    update_gui(ProcNameSerialCom.CHECKCOMPORTS, "The Vaisala Serial Port Has Unexpectedly Closed", !error_reported);
                    return false;
                }
                catch (IOException)
                {
                    is_open = false;
                    s_port.Close();
                    s_port.Dispose();
                    Thread.Sleep(1000);
                    current_exe_stage = ProcNameSerialCom.CHECKCOMPORTS;
                    update_gui(ProcNameSerialCom.CHECKCOMPORTS, "The Vaisala Serial Port Has Unexpectedly Closed", !error_reported);
                    return false;
                }
                catch (Exception e)
                {
                    is_open = false;
                    s_port.Close();
                    s_port.Dispose();
                    Thread.Sleep(1000);
                    current_exe_stage = ProcNameSerialCom.CHECKCOMPORTS;
                    update_gui(ProcNameSerialCom.CHECKCOMPORTS, e.ToString(), !error_reported);
                    return false;
                }
            }
            else
            {
       
                s_port.Dispose();
                is_open = false;
                current_exe_stage = ProcNameSerialCom.CHECKCOMPORTS;
                return false;
            }
                
            
            
        
        }
        public void Write(string command)
        {
            s_port.Write(command);
        }
        
        public bool ClearError()
        {
            //clear the error
            try
            {
                s_port.Write("reset\r");
                string line = s_port.ReadLine();
                if (line.Contains("PTB220")) return true;
                else
                {
                    current_exe_stage = ProcNameSerialCom.CHECKCOMPORTS;  //return to the init state
                    return false;
                }
            }
            catch (IOException)
            {
                s_port.Close();
                Thread.Sleep(1000);
                current_exe_stage = ProcNameSerialCom.CHECKCOMPORTS;  //return to the init state
                return false;
            }
            catch (TimeoutException)
            {
                s_port.Close();
                Thread.Sleep(1000);
                current_exe_stage = ProcNameSerialCom.CHECKCOMPORTS;  //return to the init state
                return false;
            }
            
        }


        public double ParseForResult(string line)
        {
            if (line.Contains("hPa"))
            {
                string substring = line.Substring(0, line.IndexOf('h'));
                try
                {
                    result = Convert.ToDouble(substring);
                }
                catch (FormatException e)
                {
                    ClearError();
                    update_gui(ProcNameSerialCom.POLL, e.ToString(), !error_reported);
                }

                return result;
            }
            else return 0.00;
        }

        public void Query()
        {
            int c = 0;
            while (true)
            {
                Thread.Sleep(1000); //stop the thread from thrashing
                switch (current_exe_stage)
                {
                    case ProcNameSerialCom.CHECKCOMPORTS:
                        //We only execute if an error has not been reported to the gui

                        if (c > 100)
                        {
                            //check if something has been unplugged or plugged
                           
                            c = 0;

                        }
                        //if (!ErrorReported)
                       // {
                            if (CheckComPorts()){
                                //we have a com port assigned and valid communication so we can move on
                                current_exe_stage = ProcNameSerialCom.POLL;
                            }
                        //}
                        break;
                    case ProcNameSerialCom.POLL:
                        
                        if (Read())
                        {
                            update_gui(ProcNameSerialCom.POLL, "No Error", false);
                        }
                        else current_exe_stage = ProcNameSerialCom.CHECKCOMPORTS;


                        break;
                    case ProcNameSerialCom.IDLE:
                        Thread.Sleep(1000); 
                        break;
                    default:
                        break;
                }
                c++;
            }
        }
    }
}
