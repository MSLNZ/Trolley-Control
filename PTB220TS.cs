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


   

    

    public class PTB220TS:Barometer
    {

        private Thread serialPortThread;
        private SerialPortWatcher watcher;
        public static SerialPort s_port;
        protected VaisalaUpdateGui update_gui;
        
        
        protected bool is_open = false;
        protected string last_error = "ERROR code not specified";
        



        public PTB220TS(ref VaisalaUpdateGui pbarug)
        {
            update_gui = pbarug;
            error_reported = false;
            //m = m_;
            slope = false;
            rising_falling_valid = false;
            //thread to control and monitor the barometer
            serialPortThread = new Thread(new ThreadStart(Query));
            //watcher = new SerialPortWatcher();
            current_exe_stage = BarometerExecutionStage.INIT;
            serialPortThread.Start();
            
            

        }

       
        public bool CheckComPorts()
        {
            try
            {
                if (!Init(CommunicationString)) throw new IOException();
            }

            catch(IOException)
            {
                //try to find a port that works!
                //get the names of all the com ports.
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

                        //update_gui(BarometerExecutionStage.SETUP, "Barometer Error - Could not find a valid serial COM port - Check the barometer is connected and there is a relevant COM port in the Windows Device manager", true);

                    }
                    Thread.Sleep(5000);
                    return false;
                }
            }
            return true;
        }

        
        public bool Init(string portname)
        {

            try
            {
                //create a new serial port
                s_port = new SerialPort();
                Thread.Sleep(1000);
                s_port.PortName = portname;
                s_port.BaudRate = 9600;
                s_port.Parity = Parity.Even;
                s_port.DataBits = 7;
                s_port.StopBits = StopBits.One;
                s_port.Handshake = Handshake.None;
                s_port.RtsEnable = false;
                s_port.DtrEnable = false;
                s_port.ReadTimeout = 5000;
                s_port.WriteTimeout = 2000;
                s_port.Open();
                s_port.DiscardInBuffer();
                s_port.Write("form\r");
                s_port.ReadLine();
                s_port.Write("4.2 P \" \" UUU \" \" 2.1 TREND \" \" UUU #r #n \r");
                s_port.Write("send\r");

                string l = s_port.ReadLine();
             

                if (l.Contains("hPa"))
                {
                    is_open = true;
                    communication_string = portname;
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
                
                //update_gui(BarometerExecutionStage.SETUP, "Barometer Error - Serial Port Already Open", !error_reported);
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                
                //update_gui(BarometerExecutionStage.SETUP, "Barometer Error - Serial Port Already Open", !error_reported);
                return false;
            }
        }


        public bool Read()
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
                    current_exe_stage = BarometerExecutionStage.SETUP;
                    return false;
                }
                catch (System.InvalidOperationException)
                {
                    is_open = false;
                    s_port.Close();
                    s_port.Dispose();
                    Thread.Sleep(1000);
                    current_exe_stage = BarometerExecutionStage.SETUP;
                    update_gui(BarometerExecutionStage.SETUP, "The Vaisala Serial Port Has Unexpectedly Closed", !error_reported);
                    return false;
                }
                catch (IOException)
                {
                    is_open = false;
                    s_port.Close();
                    s_port.Dispose();
                    Thread.Sleep(1000);
                    current_exe_stage = BarometerExecutionStage.SETUP;
                    update_gui(BarometerExecutionStage.SETUP, "The Vaisala Serial Port Has Unexpectedly Closed", !error_reported);
                    return false;
                }
                catch (Exception e)
                {
                    is_open = false;
                    s_port.Close();
                    s_port.Dispose();
                    Thread.Sleep(1000);
                    current_exe_stage = BarometerExecutionStage.SETUP;
                    update_gui(BarometerExecutionStage.SETUP, e.ToString(), !error_reported);
                    return false;
                }
            }
            else
            {
       
                s_port.Dispose();
                is_open = false;
                current_exe_stage = BarometerExecutionStage.SETUP;
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
                    current_exe_stage = BarometerExecutionStage.SETUP;  //return to the init state
                    return false;
                }
            }
            catch (IOException)
            {
                s_port.Close();
                Thread.Sleep(1000);
                current_exe_stage = BarometerExecutionStage.SETUP;  //return to the init state
                return false;
            }
            catch (TimeoutException)
            {
                s_port.Close();
                Thread.Sleep(1000);
                current_exe_stage = BarometerExecutionStage.SETUP;  //return to the init state
                return false;
            }
            
        }


        public double ParseForResult(string line)
        {
            if (line.Contains("hPa"))
            {
                 
                string substring = line.Remove(line.IndexOf('h'));
                if (line.Contains("-")) slope = false;
                else slope = true;

                if (line.Contains("*")) rising_falling_valid = false;
                else rising_falling_valid = true;

                if (substring[0] == '?') substring = substring.Substring(1);
                try
                {
                    result = Convert.ToDouble(substring);
                }
                catch (FormatException e)
                {
                    ClearError();
                    update_gui(BarometerExecutionStage.POLL, e.ToString(), !error_reported);
                }

                return result;
            }
            else return 0.00;
        }

        public void Query()
        {
            int c = 0;
            bool execute = true;
            while (execute)
            {
                Thread.Sleep(1000); //stop the thread from thrashing
                switch (current_exe_stage)
                {
                    case BarometerExecutionStage.INIT: //do not proceed until initialisation is complete
                        break;
                    case BarometerExecutionStage.SETUP:
                        //We only execute if an error has not been reported to the gui

                        if (c > 100) c = 0; //check if something has been unplugged or plugged
                        if (CheckComPorts()){
                              //we have a com port assigned and valid communication so we can move on
                              if(current_exe_stage!=BarometerExecutionStage.TERMINATE) current_exe_stage = BarometerExecutionStage.POLL;
                        }
                        break;
                    case BarometerExecutionStage.POLL:

                        if (Read())
                        {
                            update_gui(BarometerExecutionStage.POLL, "No Error", false);
                        }
                        else {
                            if (current_exe_stage != BarometerExecutionStage.TERMINATE) current_exe_stage = BarometerExecutionStage.SETUP;
                        }
                        break;
                    case BarometerExecutionStage.IDLE:
                        Thread.Sleep(1000); 
                        break;
                    case BarometerExecutionStage.TERMINATE:
                        execute = false;   //let this thread happily finish its operation.
                        if (s_port.IsOpen) s_port.Close();
                        break;
                    default:
                        break;
                }
                c++;
            }
        }
        public override double getPressure()
        {
            CalculateCorrection(result);
            return result + current_correction; 
        }
        public double Correction
        {
            set { current_correction = value; }
            get { return current_correction; }
        }

        

        public override bool IsOpen()
        {
             return is_open;
        }


        public string GetLastError
        {
            get
            {
                return last_error;
            }
            set
            {
                last_error = value;
            }
        }
        public override void Close()
        {
            s_port.Close();
        }
    }
}
