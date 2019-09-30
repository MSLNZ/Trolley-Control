using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;
using System.IO;
using System.Threading;
using System.Xml;
using System.Reflection;
using System.Windows;



namespace Trolley_Control
{
    public delegate void LaserUpdateGUI(short changed_proc, string laser_message, bool report_error);
    public delegate void TrolleyUpdateGUI(short procedure, object trolley_message, bool report_error);
    public delegate void MeasurementUpdateGui(short procedure, string trolley_message, bool report_error);
    public delegate void BluetoothUpdateGUI(string message);
    public delegate void THLoggerUpdateGUI(short procedure, string trolley_message, bool report_error);
    public delegate void DUTUpdateGui(short procedure, string message,bool report);
    public delegate void VaisalaUpdateGui(short procedure, string message, bool report);
    public delegate void PrintTemperatureData(double temperature, string msg, long index);
    public delegate void ShowPositions(string positions, string msg, bool report);

    public partial class Tunnel_Control_Form : Form
    {
        private XmlTextReader xmlreader;
        private XmlReaderSettings settings;
        private TemperatureMeasurement[] measurement_list;   //the current temperature measurement list
        private TemperatureMeasurement[] pending_list;       // a list of pending measurements to be added
        private Thread[] Threads;
        private static short measurement_index = 0;
        private MUX multiplexor;
        private AgilentMUX a_plexor;
        private ResistanceBridge bridge;
        private AgilentBridge a_agilent;
        private AgilentBridge b_agilent;
        private AgilentBridge c_agilent;
        private PRT[] prts;
        private bool force_update_server;
        private double OA_date;
        private long interval = 5;
        private short current_channel;
        private string ipaddress = "131.203.8.237";
        private string equiptype = "GPIB NETWORK GATEWAY";
        private string gatewaytype = "E5810A";
        private StringBuilder xdata_ = null;
        private StringBuilder ydata_ = null;
        private Thread serverUpdate;
        string xmlfilename;
        private double a1=0, a2=0, a3=0, a4=0, a5=0, a6=0, a7=0, a8=0, a9=0;
        
        private string com;
        private Laser HP5519_Laser;
        private Trolley tunnel_trolley;
        private OmegaTHLogger TH_logger1;
        private OmegaTHLogger TH_logger2;
        private VaisalaBarometer barometer;
        private Measurement[] Measurement_list;
        private int current_measurement_index;
        private int active_measurment_index;
        //private Measurement measurement;
        private Thread laserquerythread;
        private Thread trolley_move_thread;
        private Thread th_logger1_thread;
        private Thread th_logger2_thread;
        private Thread measurement_thread;
        private Thread file_opener_thread;
        private Thread painter_thread;
        
        
        //private System.IO.Stream fileStream;
        private MeasurementUpdateGui mug;
        private TrolleyUpdateGUI tug;
        private LaserUpdateGUI dlg;
        private DUTUpdateGui dutug;
        private THLoggerUpdateGUI thgui;
        private VaisalaUpdateGui pbarug;
        private Client TcpClient;
        private short mode = Mode.Debug;

        //private Message m;


        bool dialog_3_visible;
        bool dialog_4_visible;
        bool file_valid;
        bool running = false;
        private FileStream stream;
        private StreamWriter writer;
        private string path = "";

        public Tunnel_Control_Form()
        {
            InitializeComponent();
            com = "COM1";
            Bluetooth_Virtual_Serial_Port.PortName = com;
            file_valid = false;
            dialog_3_visible = false;
            dialog_4_visible = false;
            Forward_Reverse_Button.Text = "Forward";
            Go_Stop_Button.Text = "Go";
            EDMRadioButton.Checked = true;
            TotalStationRadioButton.Checked = false;
            AuxLaserRadioButton.Checked = false;

            doIni2XmlConversion();
           
            TcpClient = new Client();
            dlg = new LaserUpdateGUI(Update_lsr);   //delegate to update the gui with laser related issues
            tug = new TrolleyUpdateGUI(Update_trolley);  //delegate to update the gui with trolley related issues
            mug = new MeasurementUpdateGui(Update_measurement); //delegate to update the gui with measurement related issues
            dutug = new DUTUpdateGui(Update_DUT);  //delegate to update the gui with DUT related issues
            thgui = new THLoggerUpdateGUI(Update_THlogger); //delegate to update the gui with TH logger related issues
            pbarug = new VaisalaUpdateGui(Update_Pressure_Logger);
            //m = new Message();

            current_measurement_index = -1;
            active_measurment_index = 0;
            writer = null;
            Measurement_list = new Measurement[1];
            path = @"I:\MSL\Private\LENGTH\Edm\" + "EDMresults" + System.Environment.TickCount.ToString() + ".txt";
            

            HP5519_Laser = new Laser(ref dlg);
            tunnel_trolley = new Trolley(ref tug, ref Bluetooth_Virtual_Serial_Port);
            TH_logger1 = new OmegaTHLogger(Humidity_logger_1.Text,ref thgui);
            TH_logger1.devID = 0;
            TH_logger2 = new OmegaTHLogger(Humidity_logger_2.Text,ref thgui);
            TH_logger2.devID = 1;
            barometer = new VaisalaBarometer(ref pbarug);
            //Can have up to 100 PRTs
            prts = new PRT[100];
            //list of temperature measurements
            measurement_list = new TemperatureMeasurement[1];

            //The temperature measurement threads.
            Threads = new Thread[1];

            //Thread to control the laser
            laserquerythread = new Thread(new ParameterizedThreadStart(Laser.Query));
            laserquerythread.Start(HP5519_Laser);  // pass the laser object into the context of the new thread

            //Thread to control and monitor the state of the trolley
            trolley_move_thread = new Thread(new ParameterizedThreadStart(Trolley.Query));
            trolley_move_thread.Start(tunnel_trolley);

            //Thread to control and monitor the first temperature and humidity logger.
            th_logger1_thread = new Thread(new ParameterizedThreadStart(TH_logger1.HLoggerQuery));
            th_logger1_thread.Start(TH_logger1);

            //Thread to control and monitor the second temperature and humidity logger.
            th_logger2_thread = new Thread(new ParameterizedThreadStart(TH_logger2.HLoggerQuery));
            th_logger2_thread.Start(TH_logger2);

            Measurement_list[0] = new Measurement(ref mug, ref HP5519_Laser, ref tunnel_trolley, ref dutug,ref TH_logger1,ref TH_logger2, ref barometer);
            DUT.HostName= DUTHostName.Text;
            Measurement.SetDeviceType(Device.EDM);  //The default device under test is selected as EDM so set the first measurement DUT as EDM
           

            //start the main measurement thread, give it the first measurement in the measurement list.
            measurement_thread = new Thread(new ParameterizedThreadStart(Measurement.Measure));
            active_measurment_index = 0;
            measurement_thread.Start(Measurement_list[active_measurment_index]);

            
            Measurement.LaserWavelength = Convert.ToDouble(VacuumWavelenthTextbox.Text);
            HP5519_Laser.Wavelength = Convert.ToDouble(VacuumWavelenthTextbox.Text);

            Measurement.CO2_Concentration = Convert.ToDouble(CO2_Level.Text)/1000000;
            Measurement.DUTWavelength = Convert.ToDouble(DUT_Wavelength.Text);
            DUT.Beamfolds = 0;

            TH_logger1.HLoggerEq = Convert.ToString(H_logger_1_correction.Text);
            TH_logger1.CalculateCorrection();
            TH_logger2.HLoggerEq = Convert.ToString(H_logger_2_correction.Text);
            TH_logger2.CalculateCorrection();
            
            barometer.Correction = Convert.ToDouble(textBox3.Text);


        }

        private void Update_Pressure_Logger(short procNum, object msg, bool report)
        {
            if (this.InvokeRequired == false)
            {
                switch (procNum)
                {
                    case ProcNameSerialCom.CHECKCOMPORTS:
                        if (report)
                        {
                            barometer.ErrorReported = true;
                            DialogResult r = MessageBox.Show(msg.ToString());
                            if (r == DialogResult.OK)
                            {
                                barometer.ErrorReported = false;
                            }
                            
                        }

                        break;
                    case ProcNameSerialCom.POLL:
                        if (msg.Equals("No Error"))
                        {
                            Measurement.Pressure = barometer.getPressure;
                            redrawLaserEnviroTextbox();
                        }
                        else
                        {
                            if (report)
                            {
                                barometer.ErrorReported = true;
                                //There has been a send receive error....update the GUI
                                DialogResult r = MessageBox.Show(msg.ToString());
                                if (r == DialogResult.OK)
                                {
                                    barometer.ErrorReported = false; //Clear the error only after the user clears the dialog
                                }
                            }
                        }
                        break;
                    case ProcNameHumidity.IDLE:
                        break;
                }
            }
            else
            {
                object[] textobj = { procNum, msg, report };
                this.BeginInvoke(new VaisalaUpdateGui(Update_Pressure_Logger), textobj);
            }
        }

        private void Update_THlogger(short procNum, object msg, bool report)
        {
            if (this.InvokeRequired == false)
            {
                switch (procNum)
                {
                    case ProcNameHumidity.CONNECT:
                        if(!msg.Equals("No Error"))
                        {
                            MessageBox.Show(msg.ToString());
                        }
                        break;
                    case ProcNameHumidity.EQUATION_FORMAT:
                        if (!msg.Equals("No Error"))
                        {
                            MessageBox.Show(msg.ToString());
                        }
                        break;
                    case ProcNameHumidity.SEND_RECEIVE:
                        if (msg.Equals("No Error"))
                        {
                            //Update the GUI with the latest humidity(s)
                            
                            if (TH_logger1.isActive)
                            {
                                TH_logger1.CalculateCorrection();
                                
                            }
                            else if (TH_logger2.isActive)
                            {
                                TH_logger2.CalculateCorrection();
                            }

                            double result1 = TH_logger1.getHu();
                            double result2 = TH_logger2.getHu();
                            if (OmegaTHLogger.numConnectedLoggers == 2) Measurement.AverageHumidity = (result1 + result2) / 2;                     //we have both omega loggers working.
                            else if ((OmegaTHLogger.numConnectedLoggers == 1) && (TH_logger1.isActive)) Measurement.AverageHumidity = result1;     //only have one valid result - logger 1
                            else if ((OmegaTHLogger.numConnectedLoggers == 1) && (TH_logger2.isActive)) Measurement.AverageHumidity = result2;     //only have one valid result - logger 2
                            redrawLaserEnviroTextbox();
                        }
                        else
                        {
                            //There has been a send receive error....update the GUI
                            MessageBox.Show(msg.ToString());
                        }
                        break;
                    case ProcNameHumidity.IDLE:
                        break;
                }
            }
            else
            {
                object[] textobj = { procNum, msg, report };
                this.BeginInvoke(new THLoggerUpdateGUI(Update_THlogger), textobj);
            }
        }



        private void Update_trolley(short procNum, object msg, bool report)
        {
            if (this.InvokeRequired == false)
            {
                
               
                switch (procNum)
                {
                    case ProcNameTrolley.OPEN:
                        if (!dialog_4_visible)
                        {
                            dialog_4_visible = true;
                            DialogResult dialog_result4 = MessageBox.Show((string) msg, "ERROR", MessageBoxButtons.OK);
                            dialog_4_visible = false;
                        }
                        break;
                    case ProcNameTrolley.BLUETOOTH_CONNECTION:
                        if (report)
                        {
                            string mssg = (string)msg;
                            if(mssg =="Bluetooth Dongle not found - plug in Dongle and restart this program") tunnel_trolley.Errorstate = true;
                            MessageBox.Show((string) msg);
                        }
                        else
                        {
                            Status_Textbox.Text = (string)msg;
                        }
                        break;
                    case ProcNameTrolley.BLUETOOTH_AVAILIBLE:
                        Bluetooth_Listbox.DataSource = (List<string>) msg; 
                        break;
                    case ProcNameTrolley.IDLE:
                        break;
                }
            }
            else
            {
                object[] textobj = { procNum, msg, report };
                this.BeginInvoke(new TrolleyUpdateGUI(Update_trolley), textobj);
            }
        }

        private void Update_measurement(short procNum, string msg, bool report)
        {
            
            if (this.InvokeRequired == false)
            {
                switch (procNum)
                {
                    case ProcNameMeasurement.ISCONNECTED:
                        //check the host name
                        DUT.HostName = DUTHostName.Text;
                        DUT.TCPConnectionPending = true;

                        if (msg != "No Error")
                        {
                            MessageBox.Show(msg);
                            Measurement.CurrentExecutionStage = ExecutionStage.IDLE;
                        }
                        // else if (msg != MeasurementError.CONNECTION_ERROR)
                        // {
                        //     measurement.CurrentExecutionStage = ExecutionStage.IDLE;
                        //     MessageBox.Show(MeasurementError.CONNECTION_ERROR);
                        // }

                        break;
                    case ProcNameMeasurement.NETWORK_TRANSMISSION:
                        if ((msg != MeasurementError.NO_ERROR) && (report == true))
                        {
                            Measurement.CurrentExecutionStage = ExecutionStage.IDLE;
                            MessageBox.Show(msg);

                        }
                        break;
                    case ProcNameMeasurement.EDM_BEAM_TEMPERATURE:
                        if ((msg != MeasurementError.NO_ERROR)&& (report == true))
                        {
                            DialogResult dia_r = MessageBox.Show(msg);
                        }
                        break;
                    case ProcNameMeasurement.TROLLEY_SET:
                        if (msg.Equals("SPEED"))
                        {
                            Motor_Speed_Update();
                        }
                        else if (msg.Equals("REVERSE"))
                        {
                            UpdateReverse();
                        }
                        else if (msg.Equals("FORWARD"))
                        {
                            UpdateForward();
                        }
                        else if (msg.Equals("GO"))
                        {
                            UpdateGo();
                        }
                        else if (msg.Equals("STOP"))
                        {
                            UpdateStop();
                        }
                     
                        break;
                    case ProcNameMeasurement.DATA_CONVERSION:
                        if (msg == MeasurementError.EDM_FORMAT_ERROR)
                        {
                            Measurement.CurrentExecutionStage = ExecutionStage.IDLE;
                            MessageBox.Show("ERROR: EDM return data of unexpected format");
                            
                        }
                        break;
                    case ProcNameMeasurement.FILE_WRITE:
                        if (report)
                        {
                            MessageBox.Show(msg);
                        }
                        else
                        {

                           
                             if(writer==null) 
                             {
                                stream = new FileStream(path,FileMode.Append,System.Security.AccessControl.FileSystemRights.Write,FileShare.Write,1024,FileOptions.None);
                                writer = new System.IO.StreamWriter(stream,Encoding.UTF8,1024,true);
                            }
                            using (writer)
                            {
                                writer.WriteLine(msg);
                            }
                            
                        }
                        break;
                    case ProcNameMeasurement.EXECUTION_COMPLETE:

                        //kill the run thread of the measurement
                        measurement_thread.Abort();
                        
                        active_measurment_index++;
                        Measurement current_meas;
                        try
                        {
                            current_meas = (Measurement_list[active_measurment_index]);
                            measurement_thread = new Thread(new ParameterizedThreadStart(Measurement.Measure));
                            measurement_thread.Start(current_meas);
                            Measurement.executionStatus = true;
                            Measurement.CurrentExecutionStage = ExecutionStage.START;  //set the execution state flag to start
                        }
                        catch (IndexOutOfRangeException)
                        {
                            //there are no more measurements to do.
                            MessageBox.Show("All measurements are complete, if you want to do more, then you need to add them");
                            path = @"I:\MSL\Private\LENGTH\Edm\" + "EDMresults" + System.Environment.TickCount.ToString() + ".txt";
                            running = false;
                            DUT.Disconnect();
                            writer.Close();  //close the next file 
                            stream.Close();
                            writer = null;
                            active_measurment_index = 0;
                            current_measurement_index = -1;
                            file_valid = false;
                            Measurement_list = new Measurement[1];
                            Measurement_list[0] = new Measurement(ref mug, ref HP5519_Laser, ref tunnel_trolley, ref dutug, ref TH_logger1, ref TH_logger2,ref barometer);
                            measurement_thread = new Thread(new ParameterizedThreadStart(Measurement.Measure));
                            measurement_thread.Start(Measurement_list[0]);
                            DUT.TCPConnectionPending = true; ;
                            DUT.HostName = DUTHostName.Text;
                        }
                       
                        break;
                    case ProcNameMeasurement.IDLE:
                        break;
                    case ExecutionStage.START:
                        EDM_Reading.Text = msg;
                        Targets_RichTextbox.Select(0, Targets_RichTextbox.GetFirstCharIndexFromLine(1));
                        Targets_RichTextbox.SelectedText = "";

                        break;
                    case ExecutionStage.INTERDIATE:
                        EDM_Reading.Text = msg;
                        Targets_RichTextbox.Select(0, Targets_RichTextbox.GetFirstCharIndexFromLine(1));
                        Targets_RichTextbox.SelectedText = "";
                        break;
                    case ExecutionStage.END:
                        EDM_Reading.Text = msg;
                        Targets_RichTextbox.Select(0, Targets_RichTextbox.GetFirstCharIndexFromLine(1));
                        Targets_RichTextbox.SelectedText = "";
                        break;
                    case ExecutionStage.ONE_OFF:
                        EDM_Reading.Text = msg;
                        break;
                    case ProcNameMeasurement.STDEV:
                        Stdev_Textbox.Text = msg;
                        break;
                    default:                    //by default if the gui has been invoked it is because there is a new edm measurement to report in this case the message msg is the EDM value
                        
                        break;
                }
            }
            else
            {
                object[] textobj = { procNum, msg, report };
                this.BeginInvoke(new MeasurementUpdateGui(Update_measurement), textobj);
            }
        }

        private void Update_lsr(short procNum, string msg1, bool report)
        {

            if (this.InvokeRequired == false)
            {

                switch (procNum)
                {
                    case ProcName.E1735A_READ_DEVICE_COUNT:
                        DialogResult dialog_result1 = MessageBox.Show(msg1, "ERROR", MessageBoxButtons.OK);
                        laserquerythread.Abort();  //stop the laser thread
                        if(mode != Mode.Debug) Application.Exit();  //close the application, it needs to be restarted to reload the dlls

                        break;
                    case ProcName.E1735A_SELECT_DEVICE:
                        DialogResult dialog_result2 = MessageBox.Show(msg1, "ERROR", MessageBoxButtons.YesNo);
                        if (dialog_result2 == DialogResult.Yes)
                        {
                            laserquerythread.Abort();
                            Application.Exit();
                        }
                        else HP5519_Laser.ErrorState = true;   // allow this error to be reported again if it is still occuring
                        break;
                    case ProcName.E1735A_GET_ALL_REVISIONS:
                        break;
                    case ProcName.E1735A_BLINK_LED:
                        break;
                    case ProcName.E1735A_RESET_DEVICE:
                        break;
                    case ProcName.E1735A_READ_LAST_ERROR:
                        break;
                    case ProcName.E1735A_READ_SAMPLE_COUNT:
                        break;
                    case ProcName.E1735A_READ_SAMPLE:
                        Laser_Reading.Text = Convert.ToString(HP5519_Laser.R_Sample);

                        break;
                    case ProcName.E1735A_READ_ALL_SAMPLES:
                        break;
                    case ProcName.E1735A_READ_LAST_TRIGGER:
                        break;
                    case ProcName.E1735A_READ_LAST_TIMESTAMP:
                        break;
                    case ProcName.E1735A_SET_SAMPLE_TRIGGERS:
                        break;
                    case ProcName.E1735A_GET_SAMPLE_TRIGGERS:
                        break;
                    case ProcName.E1735A_SET_UP_TIMER:
                        break;
                    case ProcName.E1735A_START_TIMER:
                        break;
                    case ProcName.E1735A_STOP_TIMER:
                        break;
                    case ProcName.E1735A_READ_TIMER_SAMPLES:
                        break;
                    case ProcName.E1735A_SET_UP_AQB:
                        break;
                    case ProcName.E1735A_READ_AQB:
                        break;
                    case ProcName.E1735A_READ_SAMPLE_AND_AQB:
                        break;
                    case ProcName.E1735A_START_EXTERNAL_SAMPLING:
                        break;
                    case ProcName.E1735A_STOP_EXTERNAL_SAMPLING:
                        break;
                    case ProcName.E1735A_READ_BUTTON_CLICKED:
                        break;
                    case ProcName.E1735A_READ_BEAM_STRENGTH:

                        BeamStrength.Value = Convert.ToInt32(HP5519_Laser.B_Strength * 100);

                        if (msg1 == LaserErrorMessage.BeamStrengthLow && !dialog_3_visible)
                        {
                            dialog_3_visible = true;
                            DialogResult dialog_result3 = MessageBox.Show(msg1, "ERROR", MessageBoxButtons.OK);
                            dialog_3_visible = false;

                        }
                        else HP5519_Laser.ErrorState = true;
                        if (msg1 == LaserErrorMessage.LostEstablishedConnection)
                        {
                            DialogResult dialog_result3 = MessageBox.Show(msg1, "ERROR", MessageBoxButtons.YesNo);
                            if (dialog_result3 == DialogResult.Yes)
                            {
                                laserquerythread.Abort();
                                Environment.Exit(1);
                            }
                            else HP5519_Laser.ErrorState = true;
                        }


                        break;
                    case ProcName.E1735A_SET_OPTICS:
                        break;
                    case ProcName.E1735A_GET_OPTICS:
                        break;
                    case ProcName.E1735A_SET_PARAMETER:
                        break;
                    case ProcName.E1735A_GET_PARAMETER:

                        redrawLaserEnviroTextbox();
                        
                        break;
                    default: break;
                        



                }

            }
            else
            {
                object[] textobj = { procNum, msg1, report };
                this.BeginInvoke(new LaserUpdateGUI(Update_lsr), textobj);
            }
        }

        private void Update_DUT(short procNum, string msg1, bool report)
        {
            if (this.InvokeRequired == false)
            {
            }

            else
            {
                object[] textobj = { procNum, msg1, report };
                this.BeginInvoke(new LaserUpdateGUI(Update_lsr), textobj);
            }
        }




        private void Go_Stop_Button_Click(object sender, EventArgs e)
        {
            //if the serial port is open then send the command to go or stop the motor
            if (Go_Stop_Button.Text.Equals("Go"))
            {
                //change the text on the button to stop
                Go_Stop_Button.Text = "Stop";
                tunnel_trolley.ProcToDo = ProcNameTrolley.GO;
                
                
                //tunnel_trolley.Stop();
            }
            else if (Go_Stop_Button.Text.Equals("Stop"))
            {
                Go_Stop_Button.Text = "Go";
                //tunnel_trolley.Go();
                tunnel_trolley.ProcToDo = ProcNameTrolley.STOP;
            }
        }

        private void UpdateGo()
        {
            tunnel_trolley.ProcToDo = ProcNameTrolley.GO;
            Go_Stop_Button.Text = "Stop";
        }

        private void UpdateStop()
        {
            tunnel_trolley.ProcToDo = ProcNameTrolley.STOP;
            Go_Stop_Button.Text = "Go";
        }
        private void UpdateForward()
        {
            tunnel_trolley.ProcToDo = ProcNameTrolley.FORWARD;
            Go_Stop_Button.Text = "Reverse";
        }
        private void UpdateReverse()
        {
            tunnel_trolley.ProcToDo = ProcNameTrolley.REVERSE;
            Go_Stop_Button.Text = "Forward";
        }

        private void Forward_Reverse_Button_Click(object sender, EventArgs e)
        {
            //if the serial port is open then send the command to go or stop the motor
            if (Forward_Reverse_Button.Text.Equals("Forward"))
            {
                //change the text on the button to stop
                Forward_Reverse_Button.Text = "Reverse";
                //tunnel_trolley.Reverse();
                tunnel_trolley.ProcToDo = ProcNameTrolley.REVERSE;
            }
            else if (Forward_Reverse_Button.Text.Equals("Reverse"))
            {
                Forward_Reverse_Button.Text = "Forward";
                //tunnel_trolley.Forward();
                tunnel_trolley.ProcToDo = ProcNameTrolley.FORWARD;
            }

        }

        private void Motor_Speed_Scroll(object sender, EventArgs e)
        {

            byte[] buf;
            buf = new byte[1];
            buf[0] = (byte)(Motor_Speed.Value * -1);


            //tunnel_trolley.setSpeed(buf);
            tunnel_trolley.SpeedByte = buf;
            tunnel_trolley.ProcToDo = ProcNameTrolley.SETSPEED;

        }

        private void Motor_Speed_Update()
        {
            Motor_Speed.Value = tunnel_trolley.SpeedByte[0] * -1;
            tunnel_trolley.ProcToDo = ProcNameTrolley.SETSPEED;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFile();
        }
        private void OpenFile()
        {
            //Thread to process opening the file
            file_opener_thread = new Thread(new ParameterizedThreadStart(ReadFile));
            

            // Create an instance of the open file dialog box.
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            // Set filter options and filter index.
            openFileDialog1.Filter = "Text Files (.txt)|*.txt|All Files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.InitialDirectory = @"I:\MSL\Private\LENGTH\Edm";

            openFileDialog1.Multiselect = false;

            // Call the ShowDialog method to show the dialog box.
            DialogResult result = openFileDialog1.ShowDialog();

            // Process input if the user clicked OK.
            if (result == DialogResult.OK)
            {
                // Open the selected file to read.
                System.IO.Stream fileStream = openFileDialog1.OpenFile();
                file_opener_thread.Start(fileStream);

            }
        }

        private void ReadFile(object parameters)
        {
            System.IO.Stream fileStream = (System.IO.Stream) parameters;
            using (System.IO.StreamReader reader = new System.IO.StreamReader(fileStream))
            {
                // Read the first line from the file and write it the textbox.
                string line;
                file_valid = true;   //true unless the file read fails
                int intermediate_index = 0;


                //add a new measurement to the measurement list
                try
                {
                    current_measurement_index++;  //this is initilized to -1 so it will be 0 on first execution
                    if (current_measurement_index != 0)
                    {
                        Measurement_list[current_measurement_index] = new Measurement(ref mug, ref HP5519_Laser, ref tunnel_trolley, ref dutug, ref TH_logger1, ref TH_logger2, ref barometer);
                        DUT.HostName = DUTHostName.Text;

                        Measurement_list[current_measurement_index].NSamples = 1.0;

                    }

                }
                catch (IndexOutOfRangeException)
                {

                    Array.Resize<Measurement>(ref Measurement_list, current_measurement_index + 1);
                    Measurement_list[current_measurement_index] = new Measurement(ref mug, ref HP5519_Laser, ref tunnel_trolley, ref dutug, ref TH_logger1, ref TH_logger2, ref barometer);
                    DUT.HostName = DUTHostName.Text;
                    Measurement_list[current_measurement_index].NSamples = 1.0;

                }


                while (!reader.EndOfStream && file_valid)
                {
                    line = reader.ReadLine();
                    int index_of_colon = line.IndexOf(':');
                    string name = line.Remove(index_of_colon);
                    double number = 0;
                    try
                    {
                        number = Convert.ToDouble(line.Substring(index_of_colon + 1));
                    }
                    catch (FormatException)
                    {

                        FileLoaderOutput("", "Invalid File Format", true);
                       
                        current_measurement_index--;
                        file_valid = false;
                    }

                    if (file_valid)
                    {

                        switch (name)
                        {
                            case "START POSITION":
                                Measurement_list[current_measurement_index].StartPosition = number;
                                FileLoaderOutput(number.ToString(), "No Error", false);
                                
                                break;
                            case "INTERMEDIATE":
                                try
                                {
                                    Measurement_list[current_measurement_index].Intermediate[intermediate_index] = number;
                                    intermediate_index++;
                                    FileLoaderOutput(number.ToString(), "No Error", false);
                                   
                                }
                                catch (IndexOutOfRangeException)
                                {
                                    //array too small, so lets resize it
                                    double[] arr = Measurement_list[current_measurement_index].Intermediate;
                                    Array.Resize<double>(ref arr, intermediate_index + 1);
                                    arr[intermediate_index] = number;
                                    Measurement_list[current_measurement_index].Intermediate = arr;  //reassign the new array
                                    FileLoaderOutput(number.ToString(), "No Error", false);
                                    //Targets_RichTextbox.AppendText(number.ToString() + "\n");
                                    intermediate_index++;
                                }
                                break;
                            case "END POSITION":
                                Measurement_list[current_measurement_index].EndPos = number;
                                FileLoaderOutput(number.ToString(), "No Error", false);
                                //Targets_RichTextbox.AppendText(number.ToString() + "\n");
                                break;
                            case "DWELL TIME":
                                Measurement_list[current_measurement_index].DTime = number;
                                break;
                            case "NUMBER SAMPLES":
                                Measurement_list[current_measurement_index].NSamples = number;
                                Measurement_list[current_measurement_index].AveragingPending = true;
                                break;
                            case "DIRECTION":
                                if (number == 0) Measurement_list[current_measurement_index].Direction = "REVERSE";
                                else Measurement_list[current_measurement_index].Direction = "FORWARD";
                                break;
                            case "RESET":
                                if (number == 0) Measurement_list[current_measurement_index].DoReset = false;
                                else Measurement_list[current_measurement_index].DoReset = true;
                                break;
                            case "BEAM FOLDS":
                                int n = Convert.ToInt32(number);
                                switch (n)
                                {
                                    case 0:
                                        DUT.Beamfolds = 0;
                                        break;
                                    case 1:
                                        DUT.Beamfolds = 1;
                                        break;
                                    case 2:
                                        DUT.Beamfolds = 2;
                                        break;
                                    case 3:
                                        DUT.Beamfolds = 3;
                                        break;
                                    default:
                                        break;
                                }
                                break;
                            default:
                                break;
                        }
                        Measurement_list[current_measurement_index].NumInt = intermediate_index - 1;
                    }
                }
            }
            fileStream.Close();
        }

        private void FileLoaderOutput(string positions, string msg, bool report)
        {
            if (this.InvokeRequired == false)
            {
                if(report == true)
                {
                    MessageBox.Show(msg);
                }
                else
                {
                    Targets_RichTextbox.AppendText(positions + "\n");
                }
            }
            else
            {
                object[] textobj = { positions, msg, report };
                this.BeginInvoke(new ShowPositions(FileLoaderOutput), textobj);
            }
        }

        private void Reset_Laser_Click(object sender, EventArgs e)
        {
            HP5519_Laser.procToDo = ProcName.E1735A_RESET_DEVICE;
        }

        private void startMeasurementToolStripMenuItem_Click(object sender, EventArgs e)
        {

            short device = Device.EDM;
            // Alert the user which type of device under test is checked, give the option of cancelling
            if (EDMRadioButton.Checked)
            {
                DialogResult dia_res = MessageBox.Show("The device under test is set to EDM", "is this correct?", MessageBoxButtons.YesNo);
                if (dia_res == DialogResult.No) return;

            }
            else if (TotalStationRadioButton.Checked)
            {
                DialogResult dia_res = MessageBox.Show("The device under test is set to Total Station", "is this correct?", MessageBoxButtons.YesNo);
                if (dia_res == DialogResult.No) return;
                device = Device.TOTAL_STATION;
            }
            else
            {
                DialogResult dia_res = MessageBox.Show("The device under test is set to 2nd Laser", "is this correct?", MessageBoxButtons.YesNo);
                if (dia_res == DialogResult.No) return;
                device = Device.SECOND_LASER;
            }

            //enable the execution of all measurements in the measurement list
            if (Measurement_list[0] != null)
            {
                foreach (Measurement m in Measurement_list)
                {
                    m.AbortMeasurement = false;
                }
            }
            else
            {
                DialogResult dia_res = MessageBox.Show("The text file containing measurement info is either invalid or not yet opened.  Press OK to open a file or Cancel to do nothing.", "NOT INITIALISED", MessageBoxButtons.OKCancel);
                if (dia_res == DialogResult.OK)
                {
                    OpenFile();
                }
                else return;
            }
            

            while (true)
            {

                //If the file is valid then there is at least measurements to execute.
                if (file_valid)
                {
                    //cannot start a measurement if there is one already running
                    if (!running)
                    {
                        if (HP5519_Laser.LaserOperationState)
                        {
                            if (tunnel_trolley.isPortOpen)
                            {
                                DialogResult dia_res = MessageBox.Show("Is the trolley in the correct position to start the measurement?", "Check Trolley Position", MessageBoxButtons.YesNo);
                                if (dia_res == DialogResult.Yes)
                                {

                                    if (!measurement_thread.IsAlive)
                                    {
                                        measurement_thread = new Thread(new ParameterizedThreadStart(Measurement.Measure));
                                        measurement_thread.Start(Measurement_list[0]);
                                    }

                                    Measurement.executionStatus = true;
                                    Measurement.CurrentExecutionStage = ExecutionStage.START;  //set the execution state flag to stop
                                    break;
                                }
                                else break;

                            }
                            else
                            {
                                MessageBox.Show("Cannot start the measurement because the trolley communications are not working", "ERROR", MessageBoxButtons.OK);
                                break;
                            }
                        }
                        else
                        {
                            MessageBox.Show("Cannot start the measurement because the laser is not initialised", "ERROR", MessageBoxButtons.OK);
                            break;

                        }
                    }
                    else
                    {
                        MessageBox.Show("Measurement in progress.... abort measurement first");
                    }
                }
                else
                {
                    DialogResult dia_res = MessageBox.Show("The text file containing measurement info is either invalid or not yet opened.  Press OK to open a file or Cancel to do nothing.", "NOT INITIALISED", MessageBoxButtons.OKCancel);
                    if (dia_res == DialogResult.OK)
                    {
                        OpenFile();
                    }
                    else break;
                }
            }
        }

        private void abortMeasurementToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Measurement.CurrentExecutionStage = ExecutionStage.IDLE; //idle the measurement thread
            running = false;

            foreach (Measurement m in Measurement_list)
            {
                
                m.AbortMeasurement = true;
            }

            //kill the measurement run thread
            measurement_thread.Abort();

            if (tunnel_trolley.isPortOpen)
            {
                tunnel_trolley.ProcToDo = ProcNameTrolley.STOP;
                
            }
            else
            {
                MessageBox.Show("The connection to the trolley is lost, if the trolley is moving stop it manually");
            }
            Targets_RichTextbox.Clear();
        }

        private void EDMHostName_TextChanged(object sender, EventArgs e)
        {
            DUT.HostName = DUTHostName.Text;

            //set the connection pending flag if needed
            if (!DUT.TCPConnectionPending)
            {
                DUT.TCPConnectionPending = true;
            }
        }

        private void DUT_Read_Click(object sender, EventArgs e)
        {
            Measurement.OneMeasurement();
        }

        private void Humidity_logger_1_TextChanged(object sender, EventArgs e)
        {
            TH_logger1.setHostName(Humidity_logger_1.Text);
        }

        private void Humidity_logger_2_TextChanged(object sender, EventArgs e)
        {
            TH_logger2.setHostName(Humidity_logger_2.Text);
        }

        
        private void EDMRadioButton_CheckedChanged(object sender, EventArgs e)
        {

            if (EDMRadioButton.Checked)
            {
                Measurement.SetDeviceType(Device.EDM);
            }
        }

        private void TotalStationRadioButton_CheckedChanged(object sender, EventArgs e)
        {

            if (TotalStationRadioButton.Checked)
            {
                Measurement.SetDeviceType(Device.TOTAL_STATION);
            }
            
        }

        private void AuxLaserRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (AuxLaserRadioButton.Checked)
            {
                Measurement.SetDeviceType(Device.SECOND_LASER);
            }
        }

        private void VacuumWavelenthTextbox_TextChanged(object sender, EventArgs e)
        {
            try
            {
                HP5519_Laser.Wavelength = Convert.ToDouble(VacuumWavelenthTextbox.Text);
                Measurement.LaserWavelength = Convert.ToDouble(VacuumWavelenthTextbox.Text);

            }
            catch (FormatException)
            {
                MessageBox.Show("Invalid entry! Please enter a numeric");
            }
        }

        private void redrawLaserEnviroTextbox()
        {
            
           
            double avg_temp = Math.Round(Measurement.AverageTemperature, 2);
            double avg_temp2 = Math.Round(Measurement.AverageLaserBeamTemperature, 2);
            double avg_temp3 = Math.Round(Measurement.AverageEDMBeamTemperature, 2);
            double AverRH = Math.Round(Measurement.AverageHumidity, 2);
            double RH1_Corr = Math.Round(TH_logger1.Correction, 2);
            double RH2_Corr = Math.Round(TH_logger2.Correction, 2);


            StringBuilder text = new StringBuilder();
            
            
            try
            {

                LaserParameters.Clear();
                
                text.AppendLine("WaveLength (as read from HP5519 Laser head): " + HP5519_Laser.Wavelength.ToString() + " nm");
                text.AppendLine("DUT WaveLength: " + Measurement.DUTWavelength.ToString() + " nm");
                text.AppendLine("Average Tunnel Air Temp: " + avg_temp.ToString() + " °C");
                text.AppendLine("Average Laser Beam Temp: " + avg_temp2.ToString() + " °C");
                text.AppendLine("Average EDM Beam Temp: " + avg_temp3.ToString() + " °C");
                text.AppendLine("Air Pres: " + Measurement.Pressure.ToString() + " hPa");
                text.AppendLine("Air Pres Correction: " + barometer.Correction.ToString() + " hPa");
                text.AppendLine("Average %RH: " + AverRH.ToString() + "%");
                text.AppendLine("Humidity Logger 1 Correction: " + RH1_Corr.ToString()+"%");
                text.AppendLine("Humidity Logger 2 Correction: " + RH2_Corr.ToString() + "%");
                text.AppendLine("Agilent RI Corr: " + HP5519_Laser.AirCompensation.ToString());
                text.AppendLine("Phase Refractive Index (Laser): " + Measurement.CalculatePhaseRefractiveIndex(Measurement.LaserWavelength).ToString());
                try {
                    text.AppendLine("Group Refractive Index: " + Measurement.CalculateGroupRefractiveIndex(System.Convert.ToDouble(DUT_Wavelength.Text)));
                }
                catch (FormatException)
                {
                    text.AppendLine("Group Refractive Index: " + Measurement.CalculateGroupRefractiveIndex(850));
                }
                text.AppendLine("CO2 Level: " + Measurement.CO2.ToString() + "\n");
                

                //LaserParameters.AppendText(text.ToString());

                
                int[] a = Measurement.LaserPRTSUsed;
                if (a != null)
                {
                    string laser_prts = "";
                    for (int i = 0; i < a.Length; i++)
                    {
                        laser_prts = string.Concat(laser_prts, (a[i]+1).ToString() + ",");
                    }
                    text.AppendLine("Laser PRTs Used: " + laser_prts.ToString());

                    switch (DUT.Beamfolds)
                    {
                        case 0:
                            int[] b = Measurement.Row1PRTSUsed;
                            string one_beam_prts = "";
                            for (int i = 0; i < b.Length; i++)
                            {
                                one_beam_prts = string.Concat(one_beam_prts, (b[i]+1).ToString() + ",");
                            }
                            text.AppendLine("EDM PRTs Used: " + one_beam_prts.ToString());
                            text.AppendLine("");
                            break;
                        case 1:
                            break;
                        case 2:
                            break;
                        case 3:
                            break;

                    }
                    

                }
                
                for (int i = 0; i < 30; i++)
                {
                    text.AppendLine("Temperature PRT "+ (i+1).ToString()+": " + Measurement.getTemperature(i).ToString());
                }

                LaserParameters.AppendText(text.ToString());
            }
            catch (ObjectDisposedException)
            {
                Application.Exit();
            }
        }

        //Does an INI2XML conversion       
        private void doIni2XmlConversion()
        {
            if (mode != Mode.Debug) LaserParameters.AppendText("Attempting .ini to .xml conversion\n");  //close the application, it needs to be restarted to reload the dlls

            //calibration data file is better accessed off the C drive, so parse .ini file
            //is saved to the C drive.
            xmlfilename = @"I:\MSL\Private\LENGTH\EQUIPREG\cal_data.xml";
            string inifilename = @"I:\MSL\Private\LENGTH\EQUIPREG\cal_data.ini";

            if (INI2XML.Convert(inifilename, ref xmlfilename))
            {
                TextReader tr = new StreamReader(xmlfilename);
                tr.Close();

                if (mode != Mode.Debug) LaserParameters.AppendText("Successfully converted\n");
            }
            else
            {
                if (mode != Mode.Debug) LaserParameters.AppendText("Problem converting: file in use .... new version created\n...proceeding");
               
            }
        }

        /// <summary>
        /// Loads the xml file located on the C drive
        /// <summary>
        private void loadXML()
        {
            //create a new xml reader setting object incase we need to change settings on the fly
            settings = new XmlReaderSettings();

            //create a new xml doc
            xmlreader = new XmlTextReader(xmlfilename);

        }

        private void loadTemperatureConfigFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int size;
            string file = "";
            string text;
            bool okay = true;

            string location = "";
            string channel = "";
            string prt = "";
            string labname = "";
            string bridgename = "";
            string muxtype = "";

            

            a_plexor = new AgilentMUX(ref prts);
            multiplexor = a_plexor;

            


            //Open a config file for reading
            openConfigFile.InitialDirectory = @"I:\MSL\Private\LENGTH\Temperature Monitoring Data\Laboratory Configurations";
            openConfigFile.FileName = "Tunnel";
            DialogResult result;
            try
            {
                result = openConfigFile.ShowDialog(); // Show the dialog and get result.

            }
            catch (DirectoryNotFoundException)
            {
                MessageBox.Show("The directory the configurations are normally kept in no longer exists - please recreate:\n" +
                    @"I:\MSL\Private\LENGTH\Temperature Monitoring Data\Laboratory Configurations");
                return;
            }
           
            if (result == DialogResult.OK) // Test result.
            {
                file = openConfigFile.FileName;
                try
                {
                    text = File.ReadAllText(file);
                    size = text.Length;
                }
                catch (IOException)
                {
                    MessageBox.Show("Could not Open config file");
                }
            }
            else
            {
                okay = false;
            }
            if (okay)
            {
                    // parse the file
                    StreamReader file_reader = new StreamReader(file);
                    while (true)
                    {

                        string line_read = file_reader.ReadLine();


                        if (line_read.Contains("MEASUREMENT "))
                        {
                            continue;
                        }
                        else if (line_read.Contains("LOCATION IN LAB:"))
                        {
                            line_read = line_read.Remove(0, 16);
                            location = line_read;
                            continue;
                        }
                        else if (line_read.Contains("CHANNEL:"))
                        {
                            line_read = line_read.Remove(0, 8);
                            channel = line_read;
                            continue;
                        }
                        else if (line_read.Contains("PRT:"))
                        {
                            line_read = line_read.Remove(0, 4);
                            prt = line_read;
                            continue;
                        }
                        else if (line_read.Contains("LAB NAME:"))
                        {
                            line_read = line_read.Remove(0, 9);
                            labname = line_read;
                            continue;
                        }
                        else if (line_read.Contains("BRIDGE NAME:"))
                        {
                            line_read = line_read.Remove(0, 12);
                            bridgename = line_read;
                            c_agilent = new AgilentBridge(3, "GPIB3::", ref multiplexor);
                            double c= getBridgeCorrection(line_read);
                            c_agilent.A1Card1 = a1;
                            c_agilent.A2Card1 = a2;
                            c_agilent.A1Card2 = a4;
                            c_agilent.A2Card2 = a5;
                            c_agilent.A1Card3 = a7;
                            c_agilent.A2Card3 = a8; 
                            bridge = c_agilent;
                            bridge.A1 = a1;
                            bridge.A2 = a2;
                            bridge.A3 = a3;
                            bridge.A1 = a4;
                            bridge.A2 = a5;
                            bridge.A3 = a6;
                            bridge.A1 = a7;
                            bridge.A2 = a8;
                            bridge.A3 = a9;
                        continue;
                        }
                        else if (line_read.Contains("MUX_TYPE:"))
                        {
                            line_read = line_read.Remove(0, 9);
                            muxtype = line_read;
                            addTemperatureMeasurement(location,channel,prt,labname,bridgename,muxtype);
                            continue;
                        }
                        else if (line_read.Contains("END"))
                        {
                            break;
                        }
                        else break;

                    }

                
            }

        }

        private void addTemperatureMeasurement(string location_n,string channel_n,string prt_n,string lab_n, string bridge_n, string mux_n)
        {
            //Make a new PRT from the selected PRT drop down box based on the stuff in the xml file
            PRT got = findPRT(prt_n);
            current_channel = System.Convert.ToInt16(channel_n);

            //if the server updater thread is running stop its execution and restart is so that it has the full measurent list to work on.
            try
            {
                if (serverUpdate.IsAlive)
                {

                    serverUpdate.Abort();
                    serverUpdate = new Thread(new ParameterizedThreadStart(serverUpdater));
                }

                else
                {
                    serverUpdate = new Thread(new ParameterizedThreadStart(serverUpdater));

                }
            }
            catch (NullReferenceException)
            {
                serverUpdate = new Thread(new ParameterizedThreadStart(serverUpdater));
            }


            //remember to store the name of the PRT associated with this measurement
            //this is so that we can easily load a prt from the config file
            got.PRTName = prt_n;
            multiplexor.setProbe(got, current_channel);      //associates a probe with a channel

            //create a delegate to wait for the temperature data to come in
            PrintTemperatureData msgDelegate = new PrintTemperatureData(showTemperatureData);
            TemperatureMeasurement to_add = new TemperatureMeasurement(ref got, ref multiplexor, ref bridge, current_channel, ref msgDelegate, measurement_index);

            //set this from the gui later
            to_add.Inverval = interval;
            to_add.Date = getDT();
            to_add.LabLocation = lab_n;
            to_add.Filename = location_n;
            to_add.MUXName = mux_n;
            to_add.BridgeName = bridge_n;

            //increase the measurement list to fit the next measurement that might be added
            Array.Resize(ref measurement_list, measurement_index + 1);
            measurement_list[measurement_index] = to_add;

            //create a thread to run the measurement and log the data to C:
            Thread newthread = new Thread(new ParameterizedThreadStart(TemperatureMeasurement.singleMeasurement));
            newthread.Priority = ThreadPriority.Normal;
            newthread.IsBackground = true;
            Threads[measurement_index] = newthread;
            Array.Resize(ref Threads, measurement_index + 2);
            to_add.setThreads(Threads);
            to_add.setDirectory();   //set the directories for this measurement

            //start the new measurement
            newthread.Start(to_add);  //start the new thread and give it the measurement object
            measurement_index++;
            serverUpdate.Start(measurement_list);                //run the server updater with the latest measurement list

        }

        /// <summary>
        /// Get the information about the given PRT, create a new PRT and returns it
        /// </summary>
        /// <param name="prt_name_">The name of the PRT</param>
        private PRT findPRT(string prt_name_)
        {
            //set the reader to point at the start of the file
            loadXML();

            xmlreader.ResetState();
            //read the first node
            xmlreader.ReadStartElement();

            xmlreader.ReadToDescendant(string.Concat("prt", prt_name_));

            xmlreader.ReadToFollowing("reportnumber");
            string report_n = xmlreader.ReadElementString();
            xmlreader.ReadToFollowing("r0");
            double r0_ = System.Convert.ToDouble(xmlreader.ReadElementString());
            //xmlreader.readToFollowing("a");
            double a_ = System.Convert.ToDouble(xmlreader.ReadElementString());
            //xmlreader.ReadToFollowing("b");
            double b_ = System.Convert.ToDouble(xmlreader.ReadElementString());
            PRT selected_prt = new PRT(report_n, a_, b_, r0_);
            return selected_prt;
        }

        //function takes a string holding the value and 
        private void showTemperatureData(double temperature, string msg, long index)
        {

            if (this.InvokeRequired == false)
            {
                
                Measurement.AverageTemperature = TemperatureMeasurement.calculateAverageTemperature();   //this is the temperature from all prts in the tunnel
                double r = measurement_list[index].Result;
                Measurement.setTemperature(r, index);
                

                redrawLaserEnviroTextbox();
            }
            else
            {
                Measurement.calculateRegionalTemperatureLaserBeam(ref HP5519_Laser);
                Measurement.calculateRegionalTemperatureEDMBeam(DUT.Beamfolds, ref HP5519_Laser, ref mug);
                object[] textobj = { temperature, msg, index };
                this.BeginInvoke(new PrintTemperatureData(showTemperatureData), textobj);
            }
        }

        private DateTime getDT()
        {

            return DateTime.FromOADate(OA_date);


        }

        private double getBridgeCorrection(string bridge_name_)
        {
            //set the reader to point at the start of the file
            loadXML();

            xmlreader.ResetState();
            //read the first node
            xmlreader.ReadStartElement();
            xmlreader.ReadToNextSibling("RESISTANCEBRIDGE");
            xmlreader.ReadToDescendant(string.Concat("resistancebridge", bridge_name_));



            if (bridge_name_.Contains("F26")) {
                xmlreader.ReadToFollowing("A2");
                double A2_ = System.Convert.ToDouble(xmlreader.ReadElementString());
                return A2_;
            }

            xmlreader.ReadToFollowing("A1_1");
            a1 = xmlreader.ReadElementContentAsDouble();
            a2 = xmlreader.ReadElementContentAsDouble();
            a3 = xmlreader.ReadElementContentAsDouble();
            a4 = xmlreader.ReadElementContentAsDouble();
            a5 = xmlreader.ReadElementContentAsDouble();
            a6 = xmlreader.ReadElementContentAsDouble();
            a7 = xmlreader.ReadElementContentAsDouble();
            a8 = xmlreader.ReadElementContentAsDouble();
            a9 = xmlreader.ReadElementContentAsDouble();
            return 0.0;

            
        }

        private void serverUpdater(object stateInfo)
        {

            TemperatureMeasurement[] measurement_list_copy = ((TemperatureMeasurement[])stateInfo);

            //update the server every hour
            DateTime current_time;
            int stored_hour = (System.DateTime.Now).Hour;   //store this hour
            int stored_month = (System.DateTime.Now).Month;  //store this month
            int hour;
            int month;


            while (true)
            {
                Thread.Sleep(2000);
                current_time = System.DateTime.Now;  //the time stamp now
                hour = current_time.Hour;  //the hour now
                month = current_time.Month;   //The month now

                if (stored_hour != hour || force_update_server)
                {
                    //turn off force update server
                    force_update_server = false;

                    //do server update
                    stored_hour = (System.DateTime.Now).Hour;   //get the new hour we are in
                    int i = 0;
                    string di = "";
                    string dc = "";
                    while (i < measurement_index)
                    {
                        measurement_list_copy[i].getDirectories(ref di, ref dc);

                        //try and do a file copy until we find a way that works
                        while (true)
                        {
                            try
                            {
                                File.Copy(dc + measurement_list_copy[i].Filename + ".txt", di + measurement_list_copy[i].Filename + ".txt", true);
                                break;
                            }
                            catch (UnauthorizedAccessException)
                            {
                                //this has probably occured because someone has opened the file on the server and is looking at it, allow them to
                                //do so.  When they finally close it we can do the copy
                                continue;
                            }
                            catch (DirectoryNotFoundException)
                            {
                                //This will have occured because someone deleted the directory laid down originally by the measurement thread
                                //To overcome this we will rebuild the directory
                                System.IO.Directory.CreateDirectory(di);
                            }
                            catch (FileNotFoundException)
                            {

                                //this means the file does not exist on c:  we can't write a file that doesn't exist
                                break;
                            }
                            catch (IOException)
                            {
                                //This means we can't talk to the server, not much we can do but keep trying
                                continue;
                            }
                        }
                        Thread.Sleep(10000);  //sleep for 10 seconds and try again.
                        i++;
                    }

                }

                //if we have changed month we need to reset the directory folder
                if (stored_month != month)
                {
                    stored_month = (System.DateTime.Now).Month;   //store the new month we are in

                    for (int i = 0; i < measurement_index; i++)
                    {
                        measurement_list[i].setDirectory();
                    }

                }
            }
        }


        public void Application_ApplicationExit(object sender, EventArgs e)
        {
            if (barometer.IsOpen)
            {
                barometer.Close();
            }
        }

        private void DUT_Wavelength_TextChanged(object sender, EventArgs e)
        {
            try
            {
                Measurement.DUTWavelength = Convert.ToDouble(DUT_Wavelength.Text);
            }

            catch (FormatException)
            {
                MessageBox.Show("Invalid entry! Please enter a numeric");
            }
        }

        private void CO2_Level_TextChanged(object sender, EventArgs e)
        {
            try
            {
                Measurement.CO2 = Convert.ToDouble(CO2_Level.Text)/1000000;
            }
            
            catch (FormatException)
            {
                MessageBox.Show("Invalid entry! Please enter a numeric");
            }
        
        }

        private void DUT_Reset_Button_Click(object sender, EventArgs e)
        {
            Measurement.OneOffReset = true;
        }

        private void H_logger_1_correction_TextChanged(object sender, EventArgs e)
        {
            string equation = Convert.ToString(H_logger_1_correction.Text);
            TH_logger1.HLoggerEq = equation;
            TH_logger1.CalculateCorrection();
        }

        private void H_logger_2_correction_TextChanged(object sender, EventArgs e)
        {
            string equation = Convert.ToString(H_logger_2_correction.Text);
            TH_logger2.HLoggerEq= equation;
            TH_logger2.CalculateCorrection();
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            try {
                barometer.Correction = Convert.ToDouble(textBox3.Text);
            }
            catch (FormatException)
            {
                return;
            }
        }
    }
}
