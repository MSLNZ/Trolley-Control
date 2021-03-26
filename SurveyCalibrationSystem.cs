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
    public delegate void DUTUpdateGui(short procedure, string message, bool report);
    public delegate void VaisalaUpdateGui(short procedure, string message, bool report);
    public delegate void PrintTemperatureData(double temperature, string msg, long index);
    public delegate void GUIUpdater(string msg);
    public delegate void ShowPositions(string positions, string msg, bool report);
    public delegate void OnOK();


    public partial class Tunnel_Control_Form : Form
    {
        private XmlTextReader xmlreader;
        private static Thread thread_running;
        private XmlReaderSettings settings;
        private TemperatureMeasurement[] measurement_list;   //the current temperature measurement list
        private TemperatureMeasurement[] pending_list;       // a list of pending measurements to be added
        private Thread[] Threads;
        private static short measurement_index = 0;
        private MUX multiplexor;
        private AgilentMUX a_plexor;
        private ResistanceBridge bridge;
        private AgilentBridge c_agilent;
        private PRT[] prts;
        private bool force_update_server;
        private double OA_date;
        private double interval = 1;
        private short current_channel;
        private string ipaddress = "131.203.8.237";
        private string equiptype = "GPIB NETWORK GATEWAY";
        private string gatewaytype = "E5810A";
        private StringBuilder xdata_ = null;
        private StringBuilder ydata_ = null;
        private Thread serverUpdate;
        private int tick_count;
        string xmlfilename;
        private double a1 = 0, a2 = 0, a3 = 0, a4 = 0, a5 = 0, a6 = 0, a7 = 0, a8 = 0, a9 = 0;
        private bool temperature_thread_exists = false;
        private bool server_update_enabled = false;
        private string com;
        private Laser HP5519_Laser;
        private Trolley tunnel_trolley;
        private OmegaTHLogger TH_logger1;
        private OmegaTHLogger TH_logger2;
        private Barometer barometer;
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
        private static string[] showed_messages = { "", "", "", "", "", "", "", "", "", "" };
        private static int num_errors_reported = 0;
        PrintTemperatureData msgDelegate;


        //private System.IO.Stream fileStream;
        private MeasurementUpdateGui mug;
        private TrolleyUpdateGUI tug;
        private LaserUpdateGUI dlg;
        private DUTUpdateGui dutug;
        private THLoggerUpdateGUI thgui;
        private VaisalaUpdateGui pbarug;
        private GUIUpdater gupdate;
        private static event OnOK OnOkClick;

        private Client TcpClient;
        private short mode = Mode.Debug;
        private bool print = true;
        //private Message m;


        bool dialog_3_visible;
        bool dialog_4_visible;
        bool file_valid;
        bool running = false;
        private FileStream stream;
        private StreamWriter writer;

        private string config_filename = "";

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
            AuxLaserRadioButton.Checked = false;
            doIni2XmlConversion();

            TcpClient = new Client();
            dlg = new LaserUpdateGUI(Update_lsr);   //delegate to update the gui with laser related issues
            tug = new TrolleyUpdateGUI(Update_trolley);  //delegate to update the gui with trolley related issues
            mug = new MeasurementUpdateGui(Update_measurement); //delegate to update the gui with measurement related issues
            dutug = new DUTUpdateGui(Update_DUT);  //delegate to update the gui with DUT related issues
            thgui = new THLoggerUpdateGUI(Update_THlogger); //delegate to update the gui with TH logger related issues
            pbarug = new VaisalaUpdateGui(Update_Pressure_Logger);
            gupdate = new GUIUpdater(redrawLaserEnviroTextbox);
            msgDelegate = new PrintTemperatureData(showTemperatureData);

            //m = new Message();

            current_measurement_index = -1;
            active_measurment_index = 0;
            writer = null;
            Measurement_list = new Measurement[1];
            HP5519_Laser = new Laser(ref dlg);
            tunnel_trolley = new Trolley(ref tug, ref Bluetooth_Virtual_Serial_Port);
            TH_logger1 = new OmegaTHLogger(Humidity_logger_1.Text, ref thgui);
            TH_logger2 = new OmegaTHLogger(Humidity_logger_2.Text, ref thgui);
            barometer = new PTB220TS(ref pbarug);  //this is the default barometer user can select another later if neccessary


            //Can have up to 100 PRTs
            prts = new PRT[100];
            //list of temperature measurements
            measurement_list = new TemperatureMeasurement[1];

            //The temperature measurement threads.
            Threads = new Thread[1];


            Measurement_list[0] = new Measurement(ref mug, ref HP5519_Laser, ref tunnel_trolley, ref dutug, ref TH_logger1, ref TH_logger2, ref barometer);
            DUT.HostName = DUTHostName.Text;

            //start the main measurement thread, give it the first measurement in the measurement list.
            measurement_thread = new Thread(new ParameterizedThreadStart(Measurement.Measure));
            active_measurment_index = 0;
            Measurement_list[active_measurment_index].setThread(ref measurement_thread);
            measurement_thread.Start(Measurement_list[active_measurment_index]);

            setBarometerConfig("PTB220A");
            setHumidityConfig();
            setLaserConfig(Laser_Picker_ComboBox.Text);

            //Thread to control the laser
            laserquerythread = new Thread(new ParameterizedThreadStart(Laser.Query));
            laserquerythread.Start(HP5519_Laser);  // pass the laser object into the context of the new thread

            //Thread to control and monitor the state of the trolley
            trolley_move_thread = new Thread(new ParameterizedThreadStart(Trolley.Query));
            tunnel_trolley.TrolleyThread = trolley_move_thread;
            trolley_move_thread.Start(tunnel_trolley);


            //Thread to control and monitor the first temperature and humidity logger.
            th_logger1_thread = new Thread(new ParameterizedThreadStart(TH_logger1.HLoggerQuery));
            th_logger1_thread.Start(TH_logger1);


            //Thread to control and monitor the second temperature and humidity logger.
            th_logger2_thread = new Thread(new ParameterizedThreadStart(TH_logger2.HLoggerQuery));
            th_logger2_thread.Start(TH_logger2);

            Measurement.CO2_Concentration = Convert.ToDouble(CO2_Level.Text) / 1000000;
            Measurement.DUTWavelength = Convert.ToDouble(DUT_Wavelength.Text);
            DUT.Beamfolds = 0;

            Measurement_list[active_measurment_index].SetDeviceType(Device.EDM);  //The default device under test is selected as EDM so set the first measurement DUT as EDM

            tick_count = Environment.TickCount;



        }

        private void Update_Pressure_Logger(short procNum, object msg, bool report)
        {
            if (this.InvokeRequired == false)
            {
                switch (procNum)
                {
                    case BarometerExecutionStage.SETUP:
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
                    case BarometerExecutionStage.POLL:
                        if (msg.Equals("No Error"))
                        {
                            Measurement.Pressure = barometer.getPressure();
                            Thread printerThread;
                            try
                            {
                                printerThread = new Thread(new ThreadStart(Measurement_list[active_measurment_index].doDrawPrep));
                            }
                            catch (IndexOutOfRangeException)
                            {
                                return;
                            }
                            printerThread.Start();
                            redrawLaserEnviroTextbox(Measurement.PrintString);
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
                this.BeginInvoke(pbarug, textobj);
            }
        }

        private void Update_THlogger(short procNum, object msg, bool report)
        {
            if (this.InvokeRequired == false)
            {
                switch (procNum)
                {
                    case ProcNameHumidity.CONNECT:
                        if (!msg.Equals("No Error"))
                        {
                            Show(msg.ToString());
                        }
                        break;
                    case ProcNameHumidity.EQUATION_FORMAT:
                        if (!msg.Equals("No Error"))
                        {
                            Show(msg.ToString());
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

                            Thread printerThread;
                            try
                            {
                                printerThread = new Thread(new ThreadStart(Measurement_list[active_measurment_index].doDrawPrep));
                            }
                            catch (IndexOutOfRangeException)
                            {
                                return;
                            }
                            printerThread.Start();
                            redrawLaserEnviroTextbox(Measurement.PrintString);
                        }
                        else
                        {
                            //There has been a send receive error....update the GUI
                            Show(msg.ToString());
                        }
                        break;
                    case ProcNameHumidity.IDLE:
                        break;
                }
            }
            else
            {
                object[] textobj = { procNum, msg, report };
                this.BeginInvoke(thgui, textobj);
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
                            DialogResult dialog_result4 = MessageBox.Show((string)msg, "ERROR", MessageBoxButtons.OK);
                            dialog_4_visible = false;
                        }
                        break;
                    case ProcNameTrolley.BLUETOOTH_CONNECTION:
                        if (report)
                        {
                            string msg_ = (string)msg;
                            if (msg_ == "Bluetooth Dongle not found - plug in Dongle and restart this program") tunnel_trolley.Errorstate = true;
                            if (msg_.Contains("Connection lost")) Bluetooth_Listbox.SelectedItem = "";
                            MessageBox.Show(msg_);
                        }
                        else
                        {
                            Status_Textbox.Text = (string)msg;

                        }
                        break;
                    case ProcNameTrolley.BLUETOOTH_AVAILIBLE:
                        Bluetooth_Listbox.DataSource = (List<string>)msg;
                        break;
                    case ProcNameTrolley.BLUETOOTH_DATA:
                        BTData_TextBox.Text = (string)msg;
                        break;
                    case ProcNameTrolley.IDLE:
                        break;
                }
            }
            else
            {
                object[] textobj = { procNum, msg, report };
                this.BeginInvoke(tug, textobj);
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
                        if ((msg != MeasurementError.NO_ERROR) && (report == true))
                        {
                            Show(msg.ToString());
                        }
                        break;
                    case ProcNameMeasurement.TROLLEY_SET:
                        if (msg.Contains("SPEED"))
                        {
                            //there is some speed information to parse
                            string speed_setting = msg.Substring(5);
                            byte[] sb = new byte[1];
                            sb[0] = Convert.ToByte(speed_setting);
                            tunnel_trolley.SpeedByte = sb;

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
                            try
                            {
                                if (writer == null)
                                {
                                    stream = new FileStream(Measurement_list[active_measurment_index].LocalDrivePath, FileMode.Append, System.Security.AccessControl.FileSystemRights.Write, FileShare.Write, 1024, FileOptions.None);
                                    writer = new System.IO.StreamWriter(stream, Encoding.UTF8, 1024, true);
                                }
                                using (writer)
                                {
                                    writer.WriteLine(msg);
                                }
                            }
                            catch (IOException e)
                            {

                            }
                        }
                        break;
                    case ProcNameMeasurement.EXECUTION_COMPLETE:

                        //Close the text file
                        writer.Close();  //close the text file 
                        stream.Close();
                        writer = null;

                        //write copy the file from the c drive to the secure back up path
                        bool copy_succeeded = false;
                        bool copy_error = false;
                        while (!copy_succeeded)
                        {
                            try
                            {
                                File.Copy(Measurement_list[active_measurment_index].LocalDrivePath, Measurement_list[active_measurment_index].SecureBackupDrivePath);
                                copy_succeeded = true;
                                if (copy_error) MessageBox.Show("Successfull copy to secure backup");
                            }
                            catch (IOException e)
                            {
                                if (!copy_error) MessageBox.Show("Unable to copy results from C: to secure backup, check for drive connectivity. " +
                                     "In the meantime the program will continue attempting copy until connectivity is restored.  " +
                                     "You will be notified when a successfull copy occurs");
                                copy_error = true;  //true if one or more copy attempts fails.

                            }
                        }
                        //kill the run thread of the measurement
                        measurement_thread.Abort();

                        active_measurment_index++;
                        Measurement current_meas;
                        try
                        {
                            current_meas = (Measurement_list[active_measurment_index]);
                            measurement_thread = new Thread(new ParameterizedThreadStart(Measurement.Measure));
                            current_meas.setThread(ref measurement_thread);
                            measurement_thread.Start(current_meas);
                            Measurement.executionStatus = true;
                            Measurement.CurrentExecutionStage = ExecutionStage.START;  //set the execution state flag to start
                        }
                        catch (IndexOutOfRangeException)
                        {
                            //there are no more measurements to do.
                            MessageBox.Show("All measurements are complete, if you want to do more, then you need to add them");
                            running = false;
                            DUT.Disconnect();
                            active_measurment_index = 0;
                            current_measurement_index = -1;
                            file_valid = false;
                            Measurement_list = new Measurement[1];
                            Measurement_list[0] = new Measurement(ref mug, ref HP5519_Laser, ref tunnel_trolley, ref dutug, ref TH_logger1, ref TH_logger2, ref barometer);
                            measurement_thread = new Thread(new ParameterizedThreadStart(Measurement.Measure));
                            Measurement_list[0].setThread(ref measurement_thread);
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
                this.BeginInvoke(mug, textobj);
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
                        if (mode != Mode.Debug) Application.Exit();  //close the application, it needs to be restarted to reload the dlls

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
                        if (HP5519_Laser.R_Sample == double.NaN) break;
                        try
                        {
                            decimal s = Convert.ToDecimal(HP5519_Laser.R_Sample);
                            s = Math.Round(s, 7, MidpointRounding.AwayFromZero);
                            Laser_Reading.Text = Convert.ToString(s);
                        }
                        catch (Exception)
                        {
                            //bad reading ignore the reading
                            break;

                        }

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

                        Thread printerThread;
                        try
                        {
                            printerThread = new Thread(new ThreadStart(Measurement_list[active_measurment_index].doDrawPrep));
                        }
                        catch (IndexOutOfRangeException)
                        {
                            return;
                        }

                        printerThread.Start();
                        redrawLaserEnviroTextbox(Measurement.PrintString);

                        break;
                    default: break;




                }

            }
            else
            {
                object[] textobj = { procNum, msg1, report };
                this.BeginInvoke(dlg, textobj);
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
                this.BeginInvoke(dlg, textobj);
            }
        }



        public static void Show(string text)
        {
            System.Threading.ParameterizedThreadStart pT = new System.Threading.ParameterizedThreadStart(InvokeShow);
            pT.BeginInvoke(text, null, null);
        }

        private static void InvokeShow(object text)
        {
            bool found = false;
            int found_index = 0;
            int first_empty_index = 0;
            for (int i = 0; i < showed_messages.Length; i++)
            {
                if (showed_messages[i].Equals(""))
                {
                    first_empty_index = i;
                }
                if (showed_messages[i].Equals(text))
                {
                    //this message is already displayed on the screen
                    found = true;
                    found_index = i;
                    break;
                }

            }

            if (found) return; //don't display the message box again.
            else
            {
                showed_messages[first_empty_index] = (string)text;
                if (MessageBox.Show(text.ToString()) == DialogResult.OK)
                {

                    //ok has been pressed, so we need to remove the message from the showed messages array 
                    for (int i = 0; i < showed_messages.Length; i++)
                    {
                        //find the index of the entry-another thread may have changed the entry while the dialog box sat there waiting to be cleared
                        if (showed_messages[i].Equals(text))
                        {
                            showed_messages[i] = "";
                            break;
                        }
                    }
                }
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
            Forward_Reverse_Button.Text = "Reverse";
        }
        private void UpdateReverse()
        {
            tunnel_trolley.ProcToDo = ProcNameTrolley.REVERSE;
            Forward_Reverse_Button.Text = "Forward";
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
            openFileDialog1.InitialDirectory = @"G:\Shared drives\MSL - Length\Length\Edm\Tunnel Config\";

            openFileDialog1.Multiselect = false;

            // Call the ShowDialog method to show the dialog box.
            DialogResult result = openFileDialog1.ShowDialog();


            // Process input if the user clicked OK.
            if (result == DialogResult.OK)
            {

                // Open the selected file to read.
                System.IO.Stream fileStream = openFileDialog1.OpenFile();
                config_filename = openFileDialog1.FileName;
                file_opener_thread.Start(fileStream);

            }
        }

        private void ReadFile(object parameters)
        {
            System.IO.Stream fileStream = (System.IO.Stream)parameters;
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
                if (report == true)
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
            if (current_measurement_index == -1)
            {
                DialogResult dia_res = MessageBox.Show("You need to add a measurement", "Do you want to do this now?", MessageBoxButtons.YesNo);
                if (dia_res == DialogResult.No) return;
                else OpenFile();
            }
            if (EDMRadioButton.Checked)
            {
                DialogResult dia_res = MessageBox.Show("The device under test is set to EDM", "is the wavelength correct?", MessageBoxButtons.YesNo);
                if (dia_res == DialogResult.No) return;

            }
            //else if (TotalStationRadioButton.Checked)
            //{
            //    DialogResult dia_res = MessageBox.Show("The device under test is set to Total Station", "is the wavelength correct?", MessageBoxButtons.YesNo);
            //    if (dia_res == DialogResult.No) return;
            //    device = Device.TOTAL_STATION;
            //}
            else
            {
                DialogResult dia_res = MessageBox.Show("The device under test is set to 2nd Laser", "is the wavelength correct?", MessageBoxButtons.YesNo);
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

            Measurement_list[current_measurement_index].ConfigFileName = config_filename; //give the measurement it configuration name

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
                                        Measurement_list[current_measurement_index].setThread(ref measurement_thread);
                                        measurement_thread.IsBackground = false;
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
                Measurement_list[active_measurment_index].SetDeviceType(Device.EDM);
            }
        }

        private void TotalStationRadioButton_CheckedChanged(object sender, EventArgs e)
        {



        }

        private void AuxLaserRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (AuxLaserRadioButton.Checked)
            {
                Measurement_list[active_measurment_index].SetDeviceType(Device.SECOND_LASER);
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
        private void redrawLaserEnviroTextbox(string p)
        {

            if (this.InvokeRequired == false)
            {

                //only allow the gui to update this once every 1000 ms otherwise the control may be asked to update the text by many different GUI invokes in quick time.
                if (p != null)
                {
                    if (Environment.TickCount > (tick_count + 1000))
                    {
                        //get the lastest tickcount
                        tick_count = Environment.TickCount;

                        //update the richtextbox control
                        LaserParameters.Text = p.ToString();
                    }
                }
            }
            else
            {
                object[] textobj = { p };
                this.BeginInvoke(gupdate, textobj);
            }
        }

        //Does an INI2XML conversion       
        private void doIni2XmlConversion()
        {
            if (mode != Mode.Debug) LaserParameters.AppendText("Attempting .ini to .xml conversion\n");  //close the application, it needs to be restarted to reload the dlls

            //calibration data file is better accessed off the C drive, so parse .ini file
            //is saved to the C drive.
            xmlfilename = @"G:\Shared drives\MSL - Length\Length\EQUIPREG\XML files\cal_data.xml";
            string inifilename = @"G:\Shared drives\MSL - Length\Length\EQUIPREG\cal_data.ini";

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





            a_plexor = new AgilentMUX(ref prts);
            multiplexor = a_plexor;




            //Open a config file for reading
            openConfigFile.InitialDirectory = @"G:\Shared drives\MSL - Length\Length\Temperature Monitoring Data\Laboratory Configurations\";
            openConfigFile.FileName = "Tunnel";
            DialogResult result;
            try
            {
                result = openConfigFile.ShowDialog(); // Show the dialog and get result.

            }
            catch (DirectoryNotFoundException)
            {
                MessageBox.Show("The directory the configurations are normally kept in no longer exists - please recreate:\n" +
                     @"G:\Shared drives\MSL - Length\Length\Temperature Monitoring Data\Laboratory Configurations");
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
                Thread parser_thread = new Thread(new ParameterizedThreadStart(ParseConfig));
                parser_thread.IsBackground = true;
                parser_thread.Start(file_reader);
            }
        }
        private void ParseConfig(object file_reader_)
        {
            StreamReader file_reader = (StreamReader)file_reader_;
            string location = "";
            string channel = "";
            string prt = "";
            string labname = "";
            string bridgename = "";
            string muxtype = "";

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
                    getBridgeCorrections(line_read);
                    bridge = c_agilent;
                    continue;
                }
                else if (line_read.Contains("MUX_TYPE:"))
                {
                    line_read = line_read.Remove(0, 9);
                    muxtype = line_read;
                    addTemperatureMeasurement(location, channel, prt, labname, bridgename, muxtype);
                    continue;
                }
                else if (line_read.Contains("END"))
                {
                    break;
                }
                else break;
            }
        }

        private void addTemperatureMeasurement(string location_n, string channel_n, string prt_n, string lab_n, string bridge_n, string mux_n)
        {
            //Make a new PRT from the selected PRT drop down box based on the stuff in the xml file
            PRT got = findPRT(prt_n);
            current_channel = System.Convert.ToInt16(channel_n);

            //if the server updater thread is running stop its execution and restart it so that it has the full measurent list to work on.
            /*
            if (serverUpdate != null) { 
                if (serverUpdate.IsAlive)
                {
                    //We have a server update thread running! Stop the current server update thread and make a new one
                    server_update_enabled=false;
                    //while (serverUpdate.IsAlive) Thread.Sleep(10); 
                    serverUpdate.Join();  //wait here until the current server updater thread is stopped
                    serverUpdate = new Thread(new ParameterizedThreadStart(serverUpdater));
                    server_update_enabled = true;
                }
                else
                {
                    //if there's no thread running we need to start one
                    serverUpdate = new Thread(new ParameterizedThreadStart(serverUpdater));
                    server_update_enabled = true;
                }
            }
            else
            {
                serverUpdate = new Thread(new ParameterizedThreadStart(serverUpdater));
                server_update_enabled = true;
            }
            */
            //remember to store the name of the PRT associated with this measurement
            //this is so that we can easily load a prt from the config file
            got.PRTName = prt_n;
            multiplexor.setProbe(got, current_channel);      //associates a probe with a channel



            //create a temperature measurement object for this temperature measurement
            TemperatureMeasurement to_add = new TemperatureMeasurement(ref got, ref multiplexor, ref bridge, current_channel, measurement_index);

            //set the details of this measurement
            to_add.Inverval = interval;
            to_add.Date = getDT();
            to_add.LabLocation = lab_n;
            to_add.Filename = location_n;
            to_add.MUXName = mux_n;
            to_add.BridgeName = bridge_n;

            //increase the measurement list to fit the next measurement that might be added
            Array.Resize(ref measurement_list, measurement_index + 1);
            measurement_list[measurement_index] = to_add;


            //create a single thread to run the measurements and log the data to C: only do this once though
            if (measurement_list.Length == 30)
            {
                Thread newthread = new Thread(new ThreadStart(TemperatureMeasurement.MeasureAll));
                newthread.SetApartmentState(ApartmentState.STA); //set the appartment state for thread safety
                newthread.Priority = ThreadPriority.Normal;
                newthread.IsBackground = false;
                //Threads[measurement_index] = newthread;
                //Array.Resize(ref Threads, measurement_index + 2);
                to_add.setThread(ref newthread);
                //create a delegate to wait for the temperature data to come in...

                to_add.MsgDel(ref msgDelegate);

                //start the new measurement thread, giving it the first measurement object
                newthread.Start();  //start the new thread and give it the measurement object

                serverUpdate = new Thread(new ParameterizedThreadStart(serverUpdater));
                server_update_enabled = true;

            }
            to_add.setDirectory();   //set the directories for this measurement
            measurement_index++;

            if (measurement_list.Length == 30) serverUpdate.Start(measurement_list);                //run the server updater with the latest measurement list
        }

        private void serverUpdater(object stateInfo)
        {

            TemperatureMeasurement[] measurement_list_copy = ((TemperatureMeasurement[])stateInfo);



            //update the server every hour
            DateTime current_time;
            int stored_hour = (System.DateTime.Now).Hour;   //store this hour
            int stored_month = (System.DateTime.Now).Month;  //store this month
            int stored_minute = (System.DateTime.Now).Minute;  //store this month
            int hour;
            int month;
            int minute;


            while (server_update_enabled)
            {
                Thread.CurrentThread.Join(2000);
                //Thread.Sleep(2000);
                current_time = System.DateTime.Now;  //the time stamp now
                hour = current_time.Hour;  //the hour now
                month = current_time.Month;   //The month now
                minute = current_time.Minute;

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
                        Thread.CurrentThread.Join(10000);  //sleep for 10 seconds and try again.
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

                Thread printerThread;
                try
                {

                    printerThread = new Thread(new ThreadStart(Measurement_list[active_measurment_index].doDrawPrep));
                }
                catch (IndexOutOfRangeException)
                {
                    return;
                }
                printerThread.Start();
                redrawLaserEnviroTextbox(Measurement.PrintString);
            }
            else
            {
                Measurement.calculateRegionalTemperatureLaserBeam(ref HP5519_Laser);
                Measurement.calculateRegionalTemperatureEDMBeam(DUT.Beamfolds, ref HP5519_Laser, ref mug);
                object[] textobj = { temperature, msg, index };
                this.BeginInvoke(msgDelegate, textobj);
            }
        }

        private DateTime getDT()
        {

            return DateTime.FromOADate(OA_date);


        }

        private void getBridgeCorrections(string bridge_name_)
        {
            //set the reader to point at the start of the file
            loadXML();

            xmlreader.ResetState();
            //read the first node
            xmlreader.ReadStartElement();
            xmlreader.ReadToNextSibling("RESISTANCEBRIDGE");
            xmlreader.ReadToDescendant(string.Concat("resistancebridge", bridge_name_));
            xmlreader.ReadToFollowing("A1_1");
            c_agilent.A1Card1 = xmlreader.ReadElementContentAsDouble();
            c_agilent.A2Card1 = xmlreader.ReadElementContentAsDouble();
            c_agilent.A3Card1 = xmlreader.ReadElementContentAsDouble();
            c_agilent.A1Card2 = xmlreader.ReadElementContentAsDouble();
            c_agilent.A2Card2 = xmlreader.ReadElementContentAsDouble();
            c_agilent.A3Card2 = xmlreader.ReadElementContentAsDouble();
            c_agilent.A1Card3 = xmlreader.ReadElementContentAsDouble();
            c_agilent.A2Card3 = xmlreader.ReadElementContentAsDouble();
            c_agilent.A3Card3 = xmlreader.ReadElementContentAsDouble();


        }

        private void setBarometerConfig(string barometer_name_)
        {
            loadXML();

            xmlreader.ResetState();

            //read the first node
            xmlreader.ReadStartElement();
            xmlreader.ReadToNextSibling("BAROMETER");
            xmlreader.ReadToDescendant(string.Concat("barometer", barometer_name_));
            xmlreader.ReadToFollowing("reportnumber");
            barometer.ReportNum = xmlreader.ReadElementContentAsString();
            barometer.ReportDate = xmlreader.ReadElementContentAsString();
            barometer.EquipRegisterID = xmlreader.ReadElementContentAsString();
            xmlreader.ReadElementContentAsString();
            barometer.CommunicationString = xmlreader.ReadElementContentAsString();
            xmlreader.ReadElementContentAsString();
            string[] corr_strings = new string[11];
            for (int i = 0; i < 11; i++)
            {
                corr_strings[i] = xmlreader.ReadElementContentAsString();
            }
            barometer.ParseCorrectionStrings(corr_strings);
            barometer.setExecStage(BarometerExecutionStage.SETUP);


        }

        private void setHumidityConfig()
        {
            bool foundloggers = false;
            int num_loggers_found = 0;
            string r_num = "";
            string r_date = "";
            string e_id = "";
            string type = "";
            string ip = "";
            string location = "";
            string correction = "";

            //find up to two loggers with location tunnel.
            loadXML();
            xmlreader.ResetState();

            //read the first node
            xmlreader.ReadStartElement();
            xmlreader.ReadToNextSibling("HUMIDITY");
            xmlreader.Read();
            //xmlreader.ReadToDescendant(string.Concat("humidityOmega1"));  //if this ever changes we need to change the code

            while (!(xmlreader.EOF || foundloggers))
            {
                xmlreader.ReadToFollowing("reportnumber");
                try
                {
                    r_num = xmlreader.ReadElementContentAsString();
                    r_date = xmlreader.ReadElementContentAsString();
                    e_id = xmlreader.ReadElementContentAsString();
                    type = xmlreader.ReadElementContentAsString();
                    ip = xmlreader.ReadElementContentAsString();
                    location = xmlreader.ReadElementContentAsString();
                    correction = xmlreader.ReadElementContentAsString();
                }
                catch (System.InvalidOperationException)
                {
                    if (num_loggers_found == 1)
                    {
                        MessageBox.Show("Only one humidity logger was found; Non fatal, check configuration file matches real devices and their locations");
                        TH_logger2.devID = 1;
                        TH_logger2.ReportNumber = TH_logger1.ReportNumber;
                        TH_logger2.ReportDate = TH_logger1.ReportDate;
                        TH_logger2.EquipID = TH_logger1.EquipID;
                        TH_logger2.Location = TH_logger1.Location;
                        TH_logger2.Type = TH_logger1.Type;
                        TH_logger2.IP = "1.1.1.1";
                        TH_logger2.RawCorrection = TH_logger1.RawCorrection;

                        string cor = TH_logger2.RawCorrection;
                        //remove invalid part of the raw correction string.
                        while (cor.Contains("^"))
                        {

                            int removal_index = cor.IndexOf('^');
                            string c = cor.Remove(removal_index);
                            cor = string.Concat(c, cor.Substring(removal_index + 1));
                        }

                        Humidity_logger_2.Text = "1.1.1.1";
                        TH_logger2.HLoggerEq = cor;
                        TH_logger2.CalculateCorrection();

                        //Thread to control and monitor the second temperature and humidity logger.
                        th_logger2_thread = new Thread(new ParameterizedThreadStart(TH_logger2.HLoggerQuery));
                        th_logger2_thread.Start(TH_logger2);
                        foundloggers = true;
                    }

                    if (num_loggers_found == 2)
                    {
                        MessageBox.Show("No humidity loggers were found, Fatal, check configuration file matches real devices and their locations");
                    }
                    return;
                }

                if (location.Equals("TUNNEL")) //we found a humidity device which is in the tunnel...good
                {
                    num_loggers_found++;
                    if (num_loggers_found == 1)
                    {

                        TH_logger1.devID = 0;
                        TH_logger1.ReportNumber = r_num;
                        TH_logger1.ReportDate = r_date;
                        TH_logger1.EquipID = e_id;
                        TH_logger1.Location = location;
                        TH_logger1.Type = type;
                        TH_logger1.IP = ip;
                        TH_logger1.RawCorrection = correction;

                        //remove invalid part of the raw correction string.
                        while (correction.Contains("^"))
                        {
                            int removal_index = correction.IndexOf('^');
                            string c = correction.Remove(removal_index);
                            correction = string.Concat(c, correction.Substring(removal_index + 1));
                        }
                        Humidity_logger_1.Text = ip;
                        TH_logger1.HLoggerEq = correction;
                        TH_logger1.CalculateCorrection();



                    }
                    else if (num_loggers_found == 2)
                    {

                        TH_logger2.devID = 1;
                        TH_logger2.ReportNumber = r_num;
                        TH_logger2.ReportDate = r_date;
                        TH_logger2.EquipID = e_id;
                        TH_logger2.Location = location;
                        TH_logger2.Type = type;
                        TH_logger2.IP = ip;
                        TH_logger2.RawCorrection = correction;

                        //remove invalid part of the raw correction string.
                        while (correction.Contains("^"))
                        {
                            int removal_index = correction.IndexOf('^');
                            string c = correction.Remove(removal_index);
                            correction = string.Concat(c, correction.Substring(removal_index + 1));
                        }

                        Humidity_logger_2.Text = ip;
                        TH_logger2.HLoggerEq = correction;
                        TH_logger2.CalculateCorrection();


                        foundloggers = true;
                    }
                    xmlreader.Read();
                }
            }
        }

        private void setLaserConfig(string laser_serial)
        {
            loadXML();

            xmlreader.ResetState();

            //read the first node
            xmlreader.ReadStartElement();
            xmlreader.ReadToNextSibling("LASERMEASUREMENTSYSTEM");
            xmlreader.ReadToDescendant(string.Concat("laserHP5519a_", laser_serial));
            xmlreader.ReadToFollowing("reportnumber");
            HP5519_Laser.ReportNumber = xmlreader.ReadElementContentAsString();
            HP5519_Laser.ReportDate = xmlreader.ReadElementContentAsString();
            HP5519_Laser.EquipID = xmlreader.ReadElementContentAsString();
            xmlreader.ReadElementContentAsString();

            try
            {
                HP5519_Laser.Wavelength = xmlreader.ReadElementContentAsDouble();
                HP5519_Laser.WarmupTime = xmlreader.ReadElementContentAsDouble();
                HP5519_Laser.Power = xmlreader.ReadElementContentAsDouble();
            }
            catch (FormatException)
            {
                MessageBox.Show("Error During Laser Config Parse. Bad Format; element could not be read as double");
                return;
            }
            VacuumWavelenthTextbox.Text = Convert.ToString(HP5519_Laser.Wavelength);
        }



        private void Laser_Picker_ComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            setLaserConfig(Laser_Picker_ComboBox.Text);
        }

        private void LaserInfoButton_Click(object sender, EventArgs e)
        {
            string i = "Report Number: " + HP5519_Laser.ReportNumber + "\n"
                     + "Report Date: " + HP5519_Laser.ReportDate + "\n"
                     + "Equipment Register ID: " + HP5519_Laser.EquipID + "\n"
                     + "Laser Wavelength: " + HP5519_Laser.Wavelength.ToString() + " nm\n"
                     + "Warmup Time: " + HP5519_Laser.WarmupTime.ToString() + "hours\n"
                     + "Power: " + HP5519_Laser.Power.ToString() + "µW\n";
            MessageBox.Show(i);

        }

        private void BarometerTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (BarometerTypeComboBox.Text.Equals("PTB220A"))
            {
                barometer = new PTB220TS(ref pbarug);
                setBarometerConfig("PTB220A");
            }
            else if (BarometerTypeComboBox.Text.Equals("PTU303"))
            {
                barometer.setExecStage(BarometerExecutionStage.TERMINATE);
                barometer = new PTU303(ref pbarug);
                setBarometerConfig("PTU303");
            }
        }




        public void Application_ApplicationExit(object sender, EventArgs e)
        {
            if (barometer.IsOpen())
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
                Measurement.CO2 = Convert.ToDouble(CO2_Level.Text) / 1000000;
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

        private void H_Logger_1_Button_Click(object sender, EventArgs e)
        {
            string i = "Report Number: " + TH_logger1.ReportNumber + "\n"
                     + "Report Date: " + TH_logger1.ReportDate + "\n"
                     + "Equipment Register ID: " + TH_logger1.EquipID + "\n"
                     + "Location: " + TH_logger1.Location.ToString() + "\n"
                     + "IP: " + TH_logger1.IP.ToString() + "\n"
                     + "Raw Correction: " + TH_logger1.RawCorrection.ToString() + "\n";
            MessageBox.Show(i);

        }

        private void H_Logger_2_Button_Click(object sender, EventArgs e)
        {
            string i = "Report Number: " + TH_logger2.ReportNumber + "\n"
                    + "Report Date: " + TH_logger2.ReportDate + "\n"
                    + "Equipment Register ID: " + TH_logger2.EquipID + "\n"
                    + "Location: " + TH_logger2.Location.ToString() + "\n"
                    + "IP: " + TH_logger2.IP.ToString() + "\n"
                    + "Raw Correction: " + TH_logger2.RawCorrection.ToString() + "\n";
            MessageBox.Show(i);
        }
    }
}