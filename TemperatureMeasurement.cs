using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;


namespace Trolley_Control
{
    //Contains all the stuff relevant to a measurement
    //The single measurement method uses thread synchronization - hence the inherit from contectboundobject
    //[synchronization]
    class TemperatureMeasurement
    {
        private PRT prt;
        private static Thread thread_running;
        private static TemperatureMeasurement[] current_measurements = new TemperatureMeasurement[1];
        //private static long thread_count;
        private MUX mux;
        private ResistanceBridge bridge;
        private static PrintTemperatureData data;
        private DateTime date;
        private string lab_location;
        private static int measurement_anomalies;
        private string mux_name;
        private string bridge_name;
        private string directory;
        private string directory2;
        private string filename;
        private long assigned_thread_priority;
        private static bool measurement_removed = false;  //set to true if a measurement has just been removed
        private static long removal_index = 0;
        private double result;
        private static double interval;
        private short channel_for_measurement;
        private static long measurement_index_;
        private System.DateTime date_time;
        private StringBuilder x_data;
        private StringBuilder y_data;
        private static Mutex measurementMutex = new Mutex(false);
        private static Object lockthis = new Object();
        public static Random random = new Random();


        /// <summary>
        /// Builds a measurement
        /// </summary>
        /// <param name="PRT_m">The PRT being used in the measurement</param>
        /// <param name="MUX_m">The MUX type that the PRT is pluged into</param>
        /// <param name="bridge_m">The type of resistance bridge the PRT is plugged into</param>
        /// <param name="bridge_m">The channel for the measurement</param>
        /// <param name="bridge_m">A delegate to be called when temperature data becomes available</param>
        public TemperatureMeasurement(ref PRT PRT_m, ref MUX MUX_m, ref ResistanceBridge bridge_m, short channel, long measurement_index)
        {
            prt = PRT_m;
            mux = MUX_m;
            bridge = bridge_m;
            lab_location = "";
            filename = "";
            channel_for_measurement = channel;
            result = 0.0;
            measurement_index_ = measurement_index;
            //thread_count = measurement_index+1;
            x_data = new StringBuilder("");
            y_data = new StringBuilder("");
            date = DateTime.Now;
            assigned_thread_priority = 1;  //make the fresh measurement added have the highest execution priority.

            //everytime we add a new measurement change the size of the thread array
            //Array.Resize(ref threads_running,(int) measurement_index);
            Array.Resize(ref current_measurements, (int)measurement_index + 1);
            current_measurements[measurement_index] = this;

            //Array.Resize(ref threadexecution, (int)measurement_index);

            //threadexecution[measurement_index] = true;
        }
        public void MsgDel(ref PrintTemperatureData msg)
        {
            data = msg;
        }

        //calculate the average temperature
        public static double calculateAverageTemperature()
        {
            double sum = 0.0;
            int valid_results = 0;

            for (int i = 0; i < measurement_index_; i++)
            {
                double r = current_measurements[i].Result;
                if (r > 0)                  // I assume this condition is a good enough check that we have a valid reading.
                {
                    sum = sum + r;
                    valid_results++;
                }
            }
            return sum / valid_results;
        }

        public bool measurementRemoved
        {
            get
            {
                return measurement_removed;
            }

            set
            {
                measurement_removed = value;
            }

        }

        public long measurementRemovalIndex
        {
            get
            {
                return removal_index;
            }

            set
            {
                removal_index = value;
            }

        }

        public void setThread(ref Thread add_thread)
        {
            thread_running = add_thread;  //update the threads running array
        }
        //public static bool abortThread(int index)
        //{
        //    if (threads_running[index].IsAlive)
        //    {
        //        threads_running[index].Abort();
        //        return true;
        //    }
        //    else return false;
        //}
        public double Measure()
        {
            result = bridge.getTemperature(prt, channel_for_measurement, false);
            return result;
        }

        public void getDirectories(ref string i_dir, ref string c_dir)
        {
            i_dir = directory2;
            c_dir = directory;

        }


        /// <summary>
        /// creates a directory on the C drive and the I drive for the data to go into.
        /// according to year and month and lab name...
        /// </summary>
        /// <returns>True if successfuly, or False if a problem</returns>
        public void setDirectory()
        {

            //get the date component of the directory string.  Use the current time and date for this
            DateTime date = System.DateTime.Now;
            int year = date.Year;     //the year i.e 2013I:
            int month = date.Month;   //1-12 for which month we are in
            string lb;
            switch (lab_location)
            {
                case "HILGER":
                    lb = "Hilger Lab";
                    break;
                case "LONGROOM":
                    lb = "Long Room";
                    break;
                case "LASER":
                    lb = "laser lab";
                    break;
                case "TAPETUNNEL":
                    lb = "Tunnel";
                    break;
                case "LEITZ":
                    lb = "Leitz Room";
                    break;
                default:
                    lb = "MISC";
                    break;
            }



            //The default directory is on C & G:  Each measurement in written to C when it arrives 
            directory = @"C:\Temperature Monitoring Data\" + lb + @"\" + year.ToString() + @"\" + year.ToString() + "-" + month.ToString() + @"\";
            directory2 = @"G:\Shared drives\MSL - Length\Length\Temperature Monitoring Data\" + lb + @"\" + year.ToString() + @"\" + year.ToString() + "-" + month.ToString() + @"\";
            //directory2 = @"I:\MSL\Private\LENGTH\Temperature Monitoring Data\" + lb + @"\" + year.ToString() + @"\" + year.ToString() + "-" + month.ToString() + @"\";

            //create the directories if they don't exist already
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }
            if (!System.IO.Directory.Exists(directory2))
            {
                System.IO.Directory.CreateDirectory(directory2);
            }
        }

        public double Result
        {
            get
            {
                return result;
            }
            set
            {
                result = value;
            }

        }

        public StringBuilder X
        {
            get
            {
                return x_data;
            }
        }

        public StringBuilder Y
        {
            get
            {
                return y_data;
            }
        }

        public PRT PRT
        {
            get { return prt; }
        }
        /// <summary>
        /// Gets or sets the number of measurements to do.
        /// </summary>
        public DateTime Date
        {
            set { date = value; }
            get { return date; }
        }
        /// <summary>
        /// Gets or sets the interval between each measurement
        /// </summary>
        public double Inverval
        {
            set { interval = value; }
            get { return interval; }
        }

        public string Filename
        {
            set { filename = value; }
            get { return filename; }
        }
        public string LabLocation
        {
            set { lab_location = value; }
            get { return lab_location; }
        }

        public long MeasurementIndex
        {
            get { return measurement_index_; }
        }
        public string MUXName
        {
            set { mux_name = value; }
            get { return mux_name; }
        }
        public string BridgeName
        {
            set { bridge_name = value; }
            get { return bridge_name; }
        }

        public MUX MUX
        {
            get { return mux; }
        }
        public void setMUXChannel()
        {
            bridge.setCurrentChannel(channel_for_measurement);
        }
        public short getMUXChannel()
        {
            return channel_for_measurement;
        }

        public static TemperatureMeasurement[] MeasurementList
        {
            get { return current_measurements; }
            set { current_measurements = value; }
        }

        public static void MeasureAll()
        {

            string init_string = String.Concat(TemperatureMeasurement.MeasurementList[0].bridge.GPIBSICL, TemperatureMeasurement.MeasurementList[0].bridge.GPIBAddr);
            ResistanceBridge.createInteropObject();
            TemperatureMeasurement.MeasurementList[0].bridge.InitIO(init_string);
            //TemperatureMeasurement measuring = (TemperatureMeasurement)stateInfo;

            while (TemperatureMeasurement.MeasurementList.Length != 30) TemperatureMeasurement.thread_running.Join(1000);
            //The file paths
            string path;
            string path2;

            //create a file stream writer to put the data into
            System.IO.StreamWriter writer = null;

            //record the month we are in
            int month_ = System.DateTime.Now.Month;
            thread_running.Join(3000);
            while (true)
            {

                int i = 0;
                foreach (TemperatureMeasurement current_m in TemperatureMeasurement.MeasurementList)
                {
                    TemperatureMeasurement.thread_running.Join(500);
                    bool appenditure = false;

                    path = TemperatureMeasurement.MeasurementList[i].directory + TemperatureMeasurement.MeasurementList[i].Filename + ".txt";
                    path2 = @"G:\Shared drives\MSL - Length\Length\Temperature Monitoring Data\" + TemperatureMeasurement.MeasurementList[i].Filename + "_" + System.DateTime.Now.Ticks.ToString() + ".txt";


                    try
                    {
                        //if the file exists append to it otherwise create a new file
                        if (System.IO.File.Exists(path))
                        {
                            appenditure = true;
                            writer = System.IO.File.AppendText(path);
                        }
                        else writer = System.IO.File.CreateText(path);

                    }
                    catch (System.IO.IOException e)
                    {

                        //Caused because a file is already open for editing, solve by creating a new file and appending the date onto the filename
                        writer = System.IO.File.CreateText(path2);
                    }
                    using (writer)
                    {
                        //if appending we don't need this again
                        if (!appenditure)
                        {
                            writer.WriteLine("Automatically Generated File!\n");
                        }





                        //set the current (incoming) measurements priority to be the lowest (biggest number)
                        //measuring.AssignedThreadPriority = thread_count;


                        //make the current thread wait until its priority reaches 1
                        //lock (lockthis) while (measuring.AssignedThreadPriority != 1) Monitor.Wait(lockthis);

                        //---------------------------------------------------------------------START OF CRITICAL SECTION-------------------------------------------------------------
                        //lock(lockthis)
                        //{


                        //make sure the channel is correct (it may have been changed by another thread)
                        TemperatureMeasurement.MeasurementList[i].setMUXChannel();

                        //sleep the thread for the specified dead time
                        //Thread.Sleep((int)(measuring.Inverval * 1000));

                        //take the measurement
                        double measurement_result = TemperatureMeasurement.MeasurementList[i].Measure();
                        TemperatureMeasurement.MeasurementList[i].Result = measurement_result;
                        TemperatureMeasurement.MeasurementList[i].y_data.Append(measurement_result.ToString() + ",");

                        //record the time of the measurement
                        TemperatureMeasurement.MeasurementList[i].date_time = System.DateTime.Now;
                        double ole_date = TemperatureMeasurement.MeasurementList[i].date_time.ToOADate();
                        TemperatureMeasurement.MeasurementList[i].x_data.Append(ole_date.ToString() + ",");

                        //invoke the GUI to print the temperature data
                        data(measurement_result
                             , TemperatureMeasurement.MeasurementList[i].filename + " on CH" + TemperatureMeasurement.MeasurementList[i].channel_for_measurement.ToString() + " in " + TemperatureMeasurement.MeasurementList[i].lab_location + "\n"
                             , i);

                        TemperatureMeasurement.MeasurementList[i].Result = measurement_result;

                        try
                        {
                            if ((measurement_result < 25.0) || (measurement_result > 15.0))
                            {
                                measurement_anomalies++;
                            }
                        }
                        catch (Exception)
                        {
                            continue;
                        }


                        try
                        {
                            //write the measurement to file
                            writer.WriteLine(string.Concat(measurement_result.ToString() + ", " + TemperatureMeasurement.MeasurementList[i].MUX.getCurrentChannel().ToString()
                                , "," + TemperatureMeasurement.MeasurementList[i].date_time.ToString() + ", " + TemperatureMeasurement.MeasurementList[i].lab_location
                                , ", " + TemperatureMeasurement.MeasurementList[i].Filename));
                            writer.Flush();
                        }
                        catch (System.IO.IOException)
                        {
                            MessageBox.Show("Issue writing to file - Check Drive");
                            writer.Close();
                            //writer = System.IO.File.CreateText("c:" + TemperatureMeasurement.MeasurementList[i].Filename);

                        }



                        //now that the critical section has finished let the exiting thread decrement all the measurement priorities 
                        //for (int i = 0; i < TemperatureMeasurement.ThreadCount; i++)
                        //{


                        //    current_measurements[i].AssignedThreadPriority--;
                        //    Monitor.PulseAll(lockthis);

                        //}

                        //if we have removed an item then we need to reorder the priorities
                        //if (measuring.measurementRemoved)
                        //{
                        //    measuring.measurementRemoved = false;
                        //    for (long i = measuring.measurementRemovalIndex; i < TemperatureMeasurement.ThreadCount; i++)
                        //    {


                        //        current_measurements[i].AssignedThreadPriority--;
                        //        Monitor.PulseAll(lockthis);

                        //    }
                        //}
                        writer.Close();
                    }

                    i++;
                }

            }
            //thread_count--;


        }
    }
}