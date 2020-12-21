using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Trolley_Control
{


    public struct Mode
    {
        public const short Debug = 0;
        public const short SUPPRESS_DEBUG_INFO = 1;

    }
    public struct ProcNameMeasurement
    {
        public const short ISCONNECTED = 100;
        public const short REQUEST_RESET = 101;
        public const short NETWORK_TRANSMISSION = 102;
        public const short DATA_CONVERSION = 103;
        public const short FILE_WRITE = 104;
        public const short TROLLEY_SET = 105;
        public const short EXECUTION_COMPLETE = 106;
        public const short STDEV = 107;
        public const short EDM_BEAM_TEMPERATURE = 108;
        public const short IDLE = 255;

    }

    public struct Device
    {
        public const short EDM = 0;
        public const short TOTAL_STATION = 1;
        public const short SECOND_LASER = 2;

    }

    public struct MeasurementError
    {
        public const string CONNECTION_ERROR = "Could not establish a connection to the host, check connection and host name";
        public const string EDM_FORMAT_ERROR = "Data returned from the EDM is not correctly formatted";
        public const string EDM_SEND_RECEIVE_ERROR = "Request to the EDM failed";
        public const string NO_ERROR = "No Error";
        public const string FILE_ERROR = "The measurement completed but writing the file did not work";

    }
    public struct ExecutionStage
    {
        public const short INIT = 0;
        public const short START = 1;
        public const short INTERDIATE = 2;
        public const short END = 3;
        public const short DWELL = 4;
        public const short ONE_OFF = 5;
        public const short RESET = 6;
        public const short IDLE = 255;

    }
    public class MeasurementPoint
    {
        public static double[] phase_pressures = new double[30];
        public static double[] phase_temperatures = new double[30];
        public static double[] phase_humidities = new double[30];
        public static double phase_wavelength;

        public static double[] group_temperatures1 = new double[30];
        public static double[] group_pressures1 = new double[30];
        public static double[] group_humidities1 = new double[30];
        public static double[] group_temperatures2 = new double[30];
        public static double[] group_pressures2 = new double[30];
        public static double[] group_humidities2 = new double[30];
        public static double[] group_temperatures3 = new double[30];
        public static double[] group_pressures3 = new double[30];
        public static double[] group_humidities3 = new double[30];
        public static double[] group_temperatures4 = new double[30];
        public static double[] group_pressures4 = new double[30];
        public static double[] group_humidities4 = new double[30];

        public static double group_wavelength;
        public static int RI_averaging_point;
        MeasurementPoint()
        {
        }

    }


    public class Measurement
    {
        private static bool measurement_status;  //ready to measure or not?
        private static double targt = 0;
        private bool not_reached_target;
        private double start_pos;
        private double[] start_pos_value;
        private double intermediate;
        private double end_pos;
        private double[] end_pos_value;
        private double dwell_time;
        private double num_samples;
        private int number_int_values = 1;
        private string direction = "REVERSE";
        private bool abort;
        private double[] intermediate_stops;
        private double[][] intermediate_values;
        private Trolley reftrolley;
        private Laser reflaser;
        private MeasurementUpdateGui mug;
        private static DUTUpdateGui dutug;
        private OmegaTHLogger ref_logger1;
        private OmegaTHLogger ref_logger2;
        private Barometer ref_barometer;
        private static string prt_string;
        private static DUT survey_device;
        private static bool set_averaging_pending; //a flag to allow the averaging value to be sent to the DUT.
        private static short current_execution_stage;
        private static double[] temperatures;
        private static bool edm_beam_temperature_error_reported = false;
        private static bool beam_temperature_error_reported = false;
        public static double CO2_Concentration = 450E-6;
        private static readonly double EDM_TS_default_correction = 281.772087 / 1000000;
        private static string hlogger_1_equation = "";
        private static string hlogger_2_equation = "";
        private static double hlogger1_correction = 0.0;
        private static double hlogger2_correction = 0.0;
        private static double offset = 2.0;
        private static Object lockthis = new Object();
        private static int d_type = Device.EDM;
        private static int previous_num_prts_edm = 1;
        private static int previous_num_prts_laser = 1;
        private static bool run = true;
        private string config_filename = "";
        private static bool block = false;
        private Thread measurement_thread = null;




        //these lookup tables return the index of the correct prt in the temperatures array
        public static readonly int[] prtmap_over_bench = { 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 };
        public static readonly int[] prtmap_over_walkway = { 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29 };

        public static int[] no_folds_prts;
        public static int[] fold_two_prts;
        public static int[] fold_three_prts;
        public static int[] fold_four_prts;
        public static int[] laser_prts;



        //Environmental values
        private static double humidity = 50;       //%RH
        private static double pressure = 101.400;  //kpa
        private static double phase_pressure_to_file = 101.400; //kpa
        private static double group_pressure_to_file = 101.400; //kpa
        private static double average_temperature = 20.00; //degC
        private static double average_Laser_RI = 1.0; //no units
        private static double average_laser_beam_temperature = 20.00; //degC
        private static double average_EDM_RI = 1.0; //no units
        private static double average_EDM_beam_temperature = 20.0;  //degC
        private static double vacuum_wavelength = 632.99137225; //nm
        private static double dut_wavelength = 850; //nm


        byte[] speed; //the speed of the trolley;
        private static bool do_one_off;
        private static bool one_off_reset;

        private bool doreset;
        //private  measurement_device;

        public Measurement(ref MeasurementUpdateGui mg, ref Laser lsr, ref Trolley trol, ref DUTUpdateGui dutug_, ref OmegaTHLogger logger1, ref OmegaTHLogger logger2, ref Barometer barometer)
        {
            abort = false;
            doreset = false;
            do_one_off = false;
            one_off_reset = false;
            measurement_status = false;
            not_reached_target = false;
            mug = mg;
            dutug = dutug_;
            reflaser = lsr;
            reftrolley = trol;
            start_pos = 0;
            start_pos_value = new double[3]; start_pos_value[0] = 0; start_pos_value[1] = 0; start_pos_value[2] = 0;
            speed = new byte[1];
            intermediate = 0;
            end_pos = 0;
            dwell_time = 0;
            num_samples = 1;
            set_averaging_pending = true;
            intermediate_stops = new double[1];
            intermediate_values = new double[1][];
            ref_logger1 = logger1;
            ref_logger2 = logger2;
            ref_barometer = barometer;
            Measurement.CurrentExecutionStage = ExecutionStage.IDLE;
            temperatures = new double[30]; //There are 30 prts in the tunnel

        }
        public void setThread(ref Thread m_thread)
        {
            measurement_thread = m_thread;
        }
        public static int[] LaserPRTSUsed
        {
            get { return laser_prts; }
        }

        public static int[] Row1PRTSUsed
        {
            get { return no_folds_prts; }
        }

        public static int[] Row2PRTSUsed
        {
            get { return fold_two_prts; }
        }

        public static int[] Row3PRTSUsed
        {
            get { return fold_three_prts; }
        }
        public static int[] Row4PRTSUsed
        {
            get { return fold_four_prts; }
        }

        public static double CO2
        {
            set { CO2_Concentration = value; }
            get { return CO2_Concentration; }
        }

        public static string Hlogger1Eq
        {
            set { hlogger_1_equation = value; }
            get { return hlogger_1_equation; }
        }
        public static string Hlogger2Eq
        {
            set { hlogger_2_equation = value; }
            get { return hlogger_2_equation; }
        }

        public static double HLogger1Corr
        {
            get { return hlogger1_correction; }
            set { hlogger1_correction = value; }
        }
        public static double HLogger2Corr
        {
            get { return hlogger2_correction; }
            set { hlogger2_correction = value; }
        }


        public static double AverageHumidity
        {
            set { humidity = value; }
            get { return humidity; }
        }

        public static double Pressure
        {
            set { pressure = value; }
            get { return pressure; }
        }

        public static double PhasePressureToFile
        {
            set { phase_pressure_to_file = value; }
            get { return phase_pressure_to_file; }
        }
        public static double GroupPressureToFile
        {
            set { group_pressure_to_file = value; }
            get { return group_pressure_to_file; }
        }

        public static double AverageTemperature
        {
            set { average_temperature = value; }
            get { return average_temperature; }
        }

        public static double AverageLaserBeamTemperature
        {
            set { average_laser_beam_temperature = value; }
            get { return average_laser_beam_temperature; }
        }
        public static double AverageEDMBeamTemperature
        {
            set { average_EDM_beam_temperature = value; }
            get { return average_EDM_beam_temperature; }
        }
        public static double AverageLaserRI
        {
            set { average_Laser_RI = value; }
            get { return average_Laser_RI; }
        }
        public static double AverageEDMRI
        {
            set { average_EDM_RI = value; }
            get { return average_EDM_RI; }
        }

        public static void setTemperature(double res, long idx)
        {
            temperatures[idx] = res;
        }

        public static double getTemperature(long idx)
        {
            return temperatures[idx];
        }

        public static double LaserWavelength
        {

            get { return vacuum_wavelength; }
            set { vacuum_wavelength = value; }

        }

        public static double DUTWavelength
        {

            get { return dut_wavelength; }
            set { dut_wavelength = value; }

        }

        public static string PrintString
        {
            get { return prt_string; }
            set { prt_string = value; }
        }

        public static bool MeasurementLoopEnabled
        {
            get { return run; }
            set { run = value; }
        }

        public bool AveragingPending
        {
            get
            {
                return set_averaging_pending;
            }
            set
            {
                set_averaging_pending = value;
            }
        }

        public bool DoReset
        {
            set { doreset = value; }
            get { return doreset; }
        }

        public static bool OneOffReset
        {
            set { one_off_reset = value; }
            get { return one_off_reset; }
        }

        public bool AbortMeasurement
        {
            set { abort = value; }
            get { return abort; }
        }



        public static bool executionStatus
        {
            get
            {
                return measurement_status;
            }
            set
            {
                measurement_status = value;
            }

        }

        public string ConfigFileName
        {
            set { config_filename = value; }
            get { return config_filename; }
        }



        public static short CurrentExecutionStage
        {
            get
            {
                return current_execution_stage;
            }
            set
            {
                current_execution_stage = value;
            }
        }
        //target values
        public double[] Intermediate
        {
            set
            {
                intermediate_stops = value;
            }
            get
            {
                return intermediate_stops;
            }
        }

        public double[][] getIntermediateValue
        {
            get
            {
                return intermediate_values;
            }
        }

        public int NumInt
        {
            get
            {
                return number_int_values;
            }
            set
            {
                number_int_values = value;
            }
        }


        public double StartPosition
        {

            get
            {
                return start_pos;
            }
            set
            {
                start_pos = value;
            }
        }

        public double[] StartPositionValue
        {
            get
            {
                return start_pos_value;
            }
            set
            {
                start_pos_value = value;
            }
        }

        public double IntPosition
        {

            get
            {
                return intermediate;
            }
            set
            {
                intermediate = value;
            }
        }
        public double EndPos
        {

            get
            {
                return end_pos;
            }
            set
            {
                end_pos = value;
            }
        }
        public double DTime
        {
            get
            {
                return dwell_time;
            }
            set
            {
                dwell_time = value;
            }
        }
        public double NSamples
        {
            get
            {
                return num_samples;
            }
            set
            {
                num_samples = value;
            }
        }
        public string Direction
        {
            set { direction = value; }
            get { return direction; }
        }

        public double[] EndPosValue
        {
            get
            {
                return end_pos_value;
            }
            set
            {
                end_pos_value = value;
            }

        }

        public static double Target
        {
            get { return targt; }
            set { targt = value; }
        }

        public static void OneMeasurement()
        {
            do_one_off = true;
            executionStatus = true;
        }

        /// <summary>
        /// Calculate the temperature of the air where the laser beam is passing through.
        /// </summary>
        public static double calculateRegionalTemperatureLaserBeam(ref Laser r_laser)
        {
            double sum = 0.0;
            int valid_results = 0;
            double pos = r_laser.R_Sample;   //get the current laser position to figure out how much beam to include

            //prts to include
            //prts are included at:
            //actual position of prts                 relative to laser reading (m): 0.8, 4.8, 8.8 , 12.8, 16.8, 20.8, 24.8, 28.8, 32.8, 36.8, 40.8, 44.8, 48.8, 52.8
            //position position to increase prt count relative to laser reading (m): 2.8, 6.8, 10.8, 14.8, 18.8, 22.8, 26.8, 30.8, 34.8, 38.8, 42.8, 46.8, 50.8, 54.8
            //if postion of laser is over 2.8 metres the prt count goes to 2, then every 4 m thereafter the prt count increases by 1.
            //prt name mapping is stored in prtmap array. 
            if (pos.Equals(double.NaN)) return -1;  //laser not returning a value
            int num_prts_involved = Convert.ToInt32(Math.Ceiling((Math.Abs(pos) + offset) / 4));


            laser_prts = new int[num_prts_involved];

            for (int i = 0; i < num_prts_involved; i++)
            {
                laser_prts[i] = prtmap_over_bench[i];
                double r = temperatures[prtmap_over_bench[i]];
                if (r > 0)                  //I assume this condition is a good enough check that we have a valid reading.
                {
                    sum = sum + r;
                    valid_results++;
                }
            }
            average_laser_beam_temperature = sum / valid_results;
            return average_laser_beam_temperature;
        }

        /// <summary>
        /// Calculate the average Phase RI laser beam is passing through.
        /// </summary>
        /// <param name="r_laser">reference to the laser object</param>
        /// <param name="wavelength">The vacuum wavelength of the laser</param> 
        /// <param name="store">The vacuum wavelength of the laser</param>
        public static double CalculateAverageRILaserBeam(ref Laser r_laser, double wavelength, bool store)
        {
            double sum = 0.0;
            int valid_results = 0;
            double pos = r_laser.R_Sample;   //get the current laser position to figure out how much beam to include
            double RI = 1;

            //prts to include
            //prts are included at:
            //actual position of prts                 relative to laser reading (m): 0.8, 4.8, 8.8 , 12.8, 16.8, 20.8, 24.8, 28.8, 32.8, 36.8, 40.8, 44.8, 48.8, 52.8
            //position position to increase prt count relative to laser reading (m): 2.8, 6.8, 10.8, 14.8, 18.8, 22.8, 26.8, 30.8, 34.8, 38.8, 42.8, 46.8, 50.8, 54.8
            //if postion of laser is over 2.8 metres the prt count goes to 2, then every 4 m thereafter the prt count increases by 1.
            //prt name mapping is stored in prtmap array. 
            if (pos.Equals(double.NaN)) return -1;  //laser not returning a value
            int num_prts_involved = Convert.ToInt32(Math.Ceiling((Math.Abs(pos) + offset) / 4));
            laser_prts = new int[num_prts_involved];
            try
            {
                for (int i = 0; i < num_prts_involved; i++)
                {

                    laser_prts[i] = prtmap_over_bench[i];

                    double r = temperatures[prtmap_over_bench[i]];
                    if (r > 0)                  //I assume this condition is a good enough check that we have a valid reading.
                    {
                        if (store)
                        {
                            if (i == 0)
                            {
                                //reset all the measurement point arrays to zero
                                Array.Clear(MeasurementPoint.phase_temperatures, 0, 30);
                                Array.Clear(MeasurementPoint.phase_pressures, 0, 30);
                                Array.Clear(MeasurementPoint.phase_humidities, 0, 30);
                            }

                            MeasurementPoint.phase_temperatures[prtmap_over_bench[i]] = r;  //store the temperature used for the RI calculation
                            MeasurementPoint.phase_wavelength = wavelength;
                            MeasurementPoint.phase_pressures[prtmap_over_bench[i]] = Pressure;
                            MeasurementPoint.phase_humidities[prtmap_over_bench[i]] = AverageHumidity;
                            sum = sum + CalculatePhaseRefractiveIndex(MeasurementPoint.phase_wavelength, MeasurementPoint.phase_temperatures[prtmap_over_bench[i]], MeasurementPoint.phase_pressures[prtmap_over_bench[i]], MeasurementPoint.phase_humidities[prtmap_over_bench[i]]);
                        }
                        else
                        {
                            sum = sum + CalculatePhaseRefractiveIndex(wavelength, r, Pressure, AverageHumidity);
                        }
                        valid_results++;
                    }
                }
                if (store) MeasurementPoint.RI_averaging_point = valid_results;

                if (valid_results != 0) RI = sum / valid_results;
                else RI = 1;

                average_Laser_RI = RI;
                return RI;
            }
            catch (IndexOutOfRangeException)
            {
                RI = 1;
                return RI;
            }
        }

        /// <summary>
        /// Calculates the temperature of the air where the EDM beam is passing through
        /// </summary>
        /// <param name="number_beam_folds">The number of beam folds. Minimum is 0, Maximum is 3</param>
        public static double calculateRegionalTemperatureEDMBeam(int number_beam_folds, ref Laser r_laser, ref MeasurementUpdateGui ug)
        {
            try
            {
                if (number_beam_folds > 3 || number_beam_folds < 0)
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                if (!edm_beam_temperature_error_reported)
                {
                    ug(ProcNameMeasurement.EDM_BEAM_TEMPERATURE, "The number of edm beam folds is not in the range 0 to 3", true);

                }
                //don't report this error again until have the correct number of beams
                edm_beam_temperature_error_reported = true;
                return 20.0;
            }
            edm_beam_temperature_error_reported = false;

            int num_prts_involved;
            double pos = r_laser.R_Sample;   //get the current laser position to figure out how much beam to include in the beam over bench
            double average_edm_beam_temp = 0.0;
            if (pos.Equals(double.NaN)) return -1;  //laser not returning a value

            //prts to include in average calcuation
            num_prts_involved = 15 - Convert.ToInt32(Math.Floor((Math.Abs(pos) + offset) / 4));



            double first_row_avg = 0.0;  //the average for prts included in the first beam.


            if (number_beam_folds == 0)
            {
                average_edm_beam_temp = firstBeamAverage(num_prts_involved, ref ug);



            }
            else if (number_beam_folds == 1)
            {
                first_row_avg = firstBeamAverage(num_prts_involved, ref ug);

                //The partial second fold of the beam uses prts in row 1 (row over bench) and row 2 (row over walkway).  The average of these rows is used
                //there are 12 prts in each row that are used for this part of the beam.  
                //Range for bench row is 1 to 12.  Range for walkway row is 16 to 28. -->> should correspond to number written on the prts e.g 001 002 003 etc
                //for this part of the beam we are average the two rows of prts

                int valid_results = 0;
                double second_row_avg = secondBeamAverage(ref valid_results, 12, ref ug);

                //compute the weighted average of the first and second rows
                double second_row_sum = second_row_avg * valid_results;
                double first_row_sum = first_row_avg * num_prts_involved;
                average_edm_beam_temp = (first_row_sum + second_row_sum) / (valid_results + num_prts_involved); //average beam temperature
            }
            else if (number_beam_folds == 2)
            {
                first_row_avg = firstBeamAverage(num_prts_involved, ref ug);
                //The full second fold of the beam uses all prts in row 1 (row over bench) and row 2 (row over walkway).  The average of these rows is used
                //Range for bench row is 1 to 15.  Range for walkway row is 16 to 30. -->> should correspond to number written on the prts e.g 001 002 003 etc
                //For this part of the beam we are average the two rows of prts;
                int valid_results = 0;
                double second_row_avg = secondBeamAverage(ref valid_results, 15, ref ug);

                //include the partial third beam temperatures - this only includes prts in row 2
                int valid_results2 = 0;
                double third_row_avg = thirdBeamAverage(ref valid_results2, 11, ref ug);

                //compute the weighted average of the first and second rows
                double first_row_sum = first_row_avg * num_prts_involved;
                double second_row_sum = second_row_avg * valid_results;
                double third_row_sum = third_row_avg + valid_results2;
                average_edm_beam_temp = (first_row_sum + second_row_sum + third_row_sum) / (valid_results2 + valid_results + num_prts_involved);
            }
            else if (number_beam_folds == 3)
            {
                first_row_avg = firstBeamAverage(num_prts_involved, ref ug);
                //The full second fold of the beam uses all prts in row 1 (row over bench) and row 2 (row over walkway).  The average of these rows is used
                //Range for bench row is 1 to 15.  Range for walkway row is 16 to 30. -->> should correspond to number written on the prts e.g 001 002 003 etc
                //For this part of the beam we are average the two rows of prts;


                int valid_results = 0;
                double second_row_avg = secondBeamAverage(ref valid_results, 15, ref ug);

                //include the partial third beam temperatures - this only includes prts in row 2
                int valid_results2 = 0;
                double third_row_avg = thirdBeamAverage(ref valid_results2, 15, ref ug);

                int valid_results3 = 0;
                double fourth_row_avg = fourthBeamAverage(ref valid_results3, 11, ref ug);
                //compute the weighted average of the first and second rows
                double first_row_sum = first_row_avg * num_prts_involved;
                double second_row_sum = second_row_avg * valid_results;
                double third_row_sum = third_row_avg + valid_results2;
                double fourth_row_sum = fourth_row_avg + valid_results3;
                average_edm_beam_temp = (first_row_sum + second_row_sum + third_row_sum + fourth_row_sum) / (valid_results3 + valid_results2 + valid_results + num_prts_involved);
            }
            average_EDM_beam_temperature = average_edm_beam_temp;
            return average_edm_beam_temp;
        }

        /// <summary>
        /// Calculates the average refractive index for the EDM beam;
        /// </summary>
        /// <param name="number_beam_folds">The number of beam folds. Minimum is 0, Maximum is 3</param>
        /// <param name="r_laser">reference to the laser object - its reading is used to calculate which prts are included</param>
        /// <param name="ug">reference to the gui delegate</param>
        /// <param name="wavelength">vacuum wavelength of the laser</param>
        /// <param name="is_laser">false; except special case when edm is a laserhead</param>
        /// <param name="store">if true parameter used to calculate refractive index can be written to file later-should be false if GUI is calling this function</param>
        public static double calculateAverageRIEDMBeam(int number_beam_folds, ref Laser r_laser, ref MeasurementUpdateGui ug, double wavelength, bool is_laser, bool store)
        {
            double beamRI = 0;

            try
            {
                if (number_beam_folds > 3 || number_beam_folds < 0)
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                if (!edm_beam_temperature_error_reported)
                {
                    ug(ProcNameMeasurement.EDM_BEAM_TEMPERATURE, "The number of edm beam folds is not in the range 0 to 3", true);

                }
                //don't report this error again until have the correct number of beams
                edm_beam_temperature_error_reported = true;
                return 20.0;
            }
            edm_beam_temperature_error_reported = false;

            int num_prts_involved;
            double pos = r_laser.R_Sample;   //get the current laser position to figure out how much beam to include in the beam over bench
            if (pos.Equals(double.NaN)) return -1;  //laser not returning a value

            //we got a valid sample from the laser
            //prts to include in average calcuation
            num_prts_involved = 15 - Convert.ToInt32(Math.Floor((Math.Abs(pos) + offset) / 4));

            double first_row_avg = 0.0;  //the average for prts included in the first beam.


            if (number_beam_folds == 0)
            {
                beamRI = firstAverageRI(num_prts_involved, ref ug, is_laser, store);



            }
            else if (number_beam_folds == 1)
            {

                first_row_avg = firstAverageRI(num_prts_involved, ref ug, is_laser, store);

                //The partial second fold of the beam uses prts in row 1 (row over bench) and row 2 (row over walkway).  The average of these rows is used
                //there are 11 prts in row 2 that are used.
                //Range for bench row is 1 to 15.  Range for walkway row is 16 to 20. -->> should correspond to number written on the prts e.g 001 002 003 etc
                //for this part of the beam we are averaging the two rows of prts

                int valid_results = 0;
                double second_row_avg = secondAverageRI(ref valid_results, 11, ref ug, is_laser, store);

                //compute the weighted average of the first and second rows
                double second_row_sum = second_row_avg * valid_results;
                double first_row_sum = first_row_avg * num_prts_involved;
                beamRI = (first_row_sum + second_row_sum) / (valid_results + num_prts_involved); //average beam temperature
            }
            else if (number_beam_folds == 2)
            {
                first_row_avg = firstAverageRI(num_prts_involved, ref ug, is_laser, store);
                //The full second fold of the beam uses all prts in row 1 (row over bench) and row 2 (row over walkway).  The average of these rows is used
                //Range for bench row is 1 to 15.  Range for walkway row is 16 to 30. -->> should correspond to number written on the prts e.g 001 002 003 etc
                //For this part of the beam we are average the two rows of prts;
                int valid_results = 0;
                double second_row_avg = secondAverageRI(ref valid_results, 15, ref ug, is_laser, store);

                //include the partial third beam temperatures - this only includes prts in row 2
                int valid_results2 = 0;
                double third_row_avg = thirdAverageRI(ref valid_results2, 11, ref ug, is_laser, store);

                //compute the weighted average of the first and second rows
                double first_row_sum = first_row_avg * num_prts_involved;
                double second_row_sum = second_row_avg * valid_results;
                double third_row_sum = third_row_avg + valid_results2;
                beamRI = (first_row_sum + second_row_sum + third_row_sum) / (valid_results2 + valid_results + num_prts_involved);
            }
            else if (number_beam_folds == 3)
            {
                first_row_avg = firstAverageRI(num_prts_involved, ref ug, is_laser, store);
                //The full second fold of the beam uses all prts in row 1 (row over bench) and row 2 (row over walkway).  The average of these rows is used
                //Range for bench row is 1 to 15.  Range for walkway row is 16 to 30. -->> should correspond to number written on the prts e.g 001 002 003 etc
                //For this part of the beam we are average the two rows of prts;


                int valid_results = 0;
                double second_row_avg = secondAverageRI(ref valid_results, 15, ref ug, is_laser, store);

                //include the partial third beam temperatures - this only includes prts in row 2
                int valid_results2 = 0;
                double third_row_avg = thirdAverageRI(ref valid_results2, 15, ref ug, is_laser, store);

                int valid_results3 = 0;
                double fourth_row_avg = fourthAverageRI(ref valid_results3, 11, ref ug, is_laser, store);
                //compute the weighted average of the first and second rows
                double first_row_sum = first_row_avg * num_prts_involved;
                double second_row_sum = second_row_avg * valid_results;
                double third_row_sum = third_row_avg + valid_results2;
                double fourth_row_sum = fourth_row_avg + valid_results3;
                beamRI = (first_row_sum + second_row_sum + third_row_sum + fourth_row_sum) / (valid_results3 + valid_results2 + valid_results + num_prts_involved);
            }
            average_EDM_RI = beamRI;
            return average_EDM_RI;
        }

        private static double firstBeamAverage(int num_prts, ref MeasurementUpdateGui ug)
        {
            double sum = 0.0;

            no_folds_prts = new int[num_prts];
            int valid_results = 0;
            for (int i = 0; i < num_prts; i++)
            {
                double r = temperatures[prtmap_over_bench[14 - i]];
                no_folds_prts[i] = prtmap_over_bench[14 - i];
                if (r > 15.0 && r < 25.0)                  // I assume this condition is a good enough check that we have a valid reading.
                {
                    sum = sum + r;
                    valid_results++;
                }
                else
                {
                    ug(ProcNameMeasurement.EDM_BEAM_TEMPERATURE, "A PRT has a temperature reading which is not in the range 15 to 25 degree C", !beam_temperature_error_reported);
                    beam_temperature_error_reported = true;
                }
            }
            if (valid_results == num_prts) beam_temperature_error_reported = false;
            return sum / valid_results;
        }

        private static double firstAverageRI(int num_prts, ref MeasurementUpdateGui ug, bool is_laser, bool store)
        {

            double sum = 0.0;
            no_folds_prts = new int[num_prts];
            int valid_results = 0;




            for (int i = 0; i < num_prts; i++)
            {
                double r = temperatures[prtmap_over_bench[14 - i]];
                no_folds_prts[i] = prtmap_over_bench[14 - i];
                if (r > 15.0 && r < 25.0)                  // I assume this condition is a good enough check that we have a valid reading.
                {
                    if (store)
                    {
                        //clear the array first time through
                        if (i == 0)
                        {
                            Array.Clear(MeasurementPoint.group_temperatures1, 0, 30);
                            Array.Clear(MeasurementPoint.group_pressures1, 0, 30);
                            Array.Clear(MeasurementPoint.group_humidities1, 0, 30);
                        }

                        MeasurementPoint.group_temperatures1[prtmap_over_bench[14 - i]] = r;  //store the temperature used for the RI calculation
                        MeasurementPoint.group_pressures1[prtmap_over_bench[14 - i]] = Pressure;
                        MeasurementPoint.group_humidities1[prtmap_over_bench[14 - i]] = AverageHumidity;
                        MeasurementPoint.group_wavelength = DUTWavelength;

                        //Note we still use group wavelength here because we expect the user to have entered laser wavelength in the DUT wavelength box
                        if (is_laser) sum = sum + CalculatePhaseRefractiveIndex(MeasurementPoint.group_wavelength, MeasurementPoint.group_temperatures1[prtmap_over_bench[14 - i]], MeasurementPoint.group_pressures1[prtmap_over_bench[14 - i]], MeasurementPoint.group_humidities1[prtmap_over_bench[14 - i]]);
                        else sum = sum + CalculateGroupRefractiveIndex(MeasurementPoint.group_wavelength, MeasurementPoint.group_temperatures1[prtmap_over_bench[14 - i]], MeasurementPoint.group_pressures1[prtmap_over_bench[14 - i]], MeasurementPoint.group_humidities1[prtmap_over_bench[14 - i]]);
                    }
                    else
                    {
                        if (is_laser) sum = sum + CalculatePhaseRefractiveIndex(DUTWavelength, r, Pressure, AverageHumidity);
                        else sum = sum + CalculateGroupRefractiveIndex(DUTWavelength, r, Pressure, AverageHumidity);
                    }
                    valid_results++;
                }
                else
                {
                    ug(ProcNameMeasurement.EDM_BEAM_TEMPERATURE, "A PRT has a temperature reading which is not in the range 15 to 25 degree C", !beam_temperature_error_reported);
                    beam_temperature_error_reported = true;
                }
            }
            if (valid_results == num_prts) beam_temperature_error_reported = false;
            return sum / valid_results;
        }
        private static double secondBeamAverage(ref int valid_results, int total_prts_per_row, ref MeasurementUpdateGui ug)
        {
            double sum1 = 0.0;
            double sum2 = 0.0;
            fold_two_prts = new int[total_prts_per_row * 2];
            for (int i = 0; i < total_prts_per_row; i++)
            {
                double row1 = temperatures[prtmap_over_bench[14 - i]];
                double row2 = temperatures[prtmap_over_walkway[i]];



                fold_two_prts[i] = prtmap_over_bench[14 - i];
                fold_two_prts[i + total_prts_per_row] = prtmap_over_walkway[i];

                if ((row1 > 15.0) && (row2 > 15.0) && (row1 < 25.0) && (row2 < 25.0))
                {
                    sum1 = sum1 + row1;
                    sum2 = sum2 + row2;
                    valid_results++;
                }
                else
                {
                    ug(ProcNameMeasurement.EDM_BEAM_TEMPERATURE, "A PRT has a temperature reading which is not in the range 15 to 25 degree C", !beam_temperature_error_reported);
                    beam_temperature_error_reported = true;
                }
            }
            if (valid_results == total_prts_per_row) beam_temperature_error_reported = false;
            //compute the averages
            double average1 = sum1 / valid_results;
            double average2 = sum2 / valid_results;

            //same number of points for each average so we okay to compute the average of the two prt rows
            double second_row_avg = (average1 + average2) / 2;
            return second_row_avg;
        }

        private static double secondAverageRI(ref int valid_results, int total_prts_per_row, ref MeasurementUpdateGui ug, bool is_laser, bool store)
        {
            double sum1 = 0.0;
            double sum2 = 0.0;
            fold_two_prts = new int[total_prts_per_row * 2];



            for (int i = 0; i < total_prts_per_row; i++)
            {
                double row1 = temperatures[prtmap_over_bench[14 - i]];
                double row2 = temperatures[prtmap_over_walkway[i]];

                fold_two_prts[i] = prtmap_over_bench[14 - i];
                fold_two_prts[i + total_prts_per_row] = prtmap_over_walkway[i];

                if ((row1 > 15.0) && (row2 > 15.0) && (row1 < 25.0) && (row2 < 25.0))
                {
                    if (store)
                    {
                        if (i == 0)
                        {
                            Array.Clear(MeasurementPoint.group_temperatures2, 0, 30);
                            Array.Clear(MeasurementPoint.group_pressures2, 0, 30);
                            Array.Clear(MeasurementPoint.group_humidities2, 0, 30);
                        }

                        MeasurementPoint.group_temperatures2[prtmap_over_bench[14 - i]] = row1;  //store the temperature used for the RI calculation
                        MeasurementPoint.group_temperatures2[prtmap_over_walkway[i]] = row2;  //store the temperature used for the RI calculation
                        MeasurementPoint.group_pressures2[prtmap_over_bench[14 - i]] = Pressure;
                        MeasurementPoint.group_pressures2[prtmap_over_walkway[i]] = Pressure;
                        MeasurementPoint.group_humidities2[prtmap_over_bench[14 - i]] = AverageHumidity;
                        MeasurementPoint.group_humidities2[prtmap_over_walkway[i]] = AverageHumidity;
                        MeasurementPoint.group_wavelength = DUTWavelength;

                        if (is_laser)
                        {
                            sum1 = sum1 + CalculatePhaseRefractiveIndex(MeasurementPoint.group_wavelength, MeasurementPoint.group_temperatures2[prtmap_over_bench[14 - i]], MeasurementPoint.group_pressures2[prtmap_over_bench[14 - i]], MeasurementPoint.group_humidities2[prtmap_over_bench[14 - i]]);
                            sum2 = sum2 + CalculatePhaseRefractiveIndex(MeasurementPoint.group_wavelength, MeasurementPoint.group_temperatures2[prtmap_over_walkway[i]], MeasurementPoint.group_pressures2[prtmap_over_walkway[i]], MeasurementPoint.group_humidities2[prtmap_over_walkway[i]]);
                        }
                        else
                        {
                            sum1 = sum1 + CalculateGroupRefractiveIndex(MeasurementPoint.group_wavelength, MeasurementPoint.group_temperatures2[prtmap_over_bench[14 - i]], MeasurementPoint.group_pressures2[prtmap_over_bench[14 - i]], MeasurementPoint.group_humidities2[prtmap_over_bench[14 - i]]);
                            sum2 = sum2 + CalculateGroupRefractiveIndex(MeasurementPoint.group_wavelength, MeasurementPoint.group_temperatures2[prtmap_over_walkway[i]], MeasurementPoint.group_pressures2[prtmap_over_walkway[i]], MeasurementPoint.group_humidities2[prtmap_over_walkway[i]]);
                        }
                    }
                    else
                    {
                        if (is_laser)
                        {
                            sum1 = sum1 + CalculatePhaseRefractiveIndex(DUTWavelength, row1, Pressure, AverageHumidity);
                            sum2 = sum2 + CalculatePhaseRefractiveIndex(DUTWavelength, row2, Pressure, AverageHumidity);
                        }
                        else
                        {
                            sum1 = sum1 + CalculateGroupRefractiveIndex(DUTWavelength, row1, Pressure, AverageHumidity);
                            sum2 = sum2 + CalculateGroupRefractiveIndex(DUTWavelength, row2, Pressure, AverageHumidity);
                        }
                    }
                    valid_results++;
                }
                else
                {
                    //ug(ProcNameMeasurement.EDM_BEAM_TEMPERATURE, "A PRT has a temperature reading which is not in the range 15 to 25 degree C", !beam_temperature_error_reported);
                    beam_temperature_error_reported = true;
                }
            }
            if (valid_results == total_prts_per_row) beam_temperature_error_reported = false;
            //compute the averages
            double average1 = sum1 / valid_results;
            double average2 = sum2 / valid_results;

            //same number of points for each average so we okay to compute the average of the two prt rows
            double second_row_avg = (average1 + average2) / 2;
            return second_row_avg;
        }

        private static double thirdBeamAverage(ref int valid_results, int total_prts_in_row, ref MeasurementUpdateGui ug)
        {
            double sum1 = 0.0;
            fold_three_prts = new int[total_prts_in_row];
            for (int i = 0; i < total_prts_in_row; i++)
            {
                double row2 = temperatures[prtmap_over_walkway[14 - i]];
                fold_three_prts[i] = prtmap_over_walkway[14 - i];

                if (row2 > 15.0 && row2 < 25.0)
                {
                    sum1 = sum1 + row2;
                    valid_results++;
                }
                else
                {
                    //ug(ProcNameMeasurement.EDM_BEAM_TEMPERATURE, "A PRT has a temperature reading which is not in the range 15 to 25 degree C", !beam_temperature_error_reported);
                    beam_temperature_error_reported = true;
                }
            }
            if (valid_results == total_prts_in_row) beam_temperature_error_reported = false;
            //compute the average
            double third_row = sum1 / valid_results;

            return third_row;
        }

        private static double thirdAverageRI(ref int valid_results, int total_prts_in_row, ref MeasurementUpdateGui ug, bool is_laser, bool store)
        {
            double sum1 = 0.0;
            fold_three_prts = new int[total_prts_in_row];



            for (int i = 0; i < total_prts_in_row; i++)
            {
                double row2 = temperatures[prtmap_over_walkway[14 - i]];
                fold_three_prts[i] = prtmap_over_walkway[14 - i];
                if (row2 > 15.0 && row2 < 25.0)
                {
                    if (store)
                    {
                        if (i == 0)
                        {
                            Array.Clear(MeasurementPoint.group_temperatures3, 0, 30);
                            Array.Clear(MeasurementPoint.group_pressures3, 0, 30);
                            Array.Clear(MeasurementPoint.group_humidities3, 0, 30);
                        }

                        MeasurementPoint.group_temperatures3[prtmap_over_walkway[14 - i]] = row2;  //store the temperature used for the RI calculation
                        MeasurementPoint.group_pressures3[prtmap_over_walkway[14 - i]] = Pressure;
                        MeasurementPoint.group_humidities3[prtmap_over_walkway[14 - i]] = AverageHumidity;
                        MeasurementPoint.group_wavelength = DUTWavelength;

                        if (is_laser) sum1 = sum1 + CalculatePhaseRefractiveIndex(MeasurementPoint.group_wavelength, MeasurementPoint.group_temperatures3[prtmap_over_walkway[14 - i]], MeasurementPoint.group_pressures3[prtmap_over_walkway[14 - i]], MeasurementPoint.group_humidities3[prtmap_over_walkway[14 - i]]);
                        else sum1 = sum1 + CalculateGroupRefractiveIndex(MeasurementPoint.group_wavelength, MeasurementPoint.group_temperatures3[prtmap_over_walkway[14 - i]], MeasurementPoint.group_pressures3[prtmap_over_walkway[14 - i]], MeasurementPoint.group_humidities3[prtmap_over_walkway[14 - i]]);
                        valid_results++;


                    }
                    else
                    {

                        if (is_laser) sum1 = sum1 + CalculatePhaseRefractiveIndex(DUTWavelength, row2, Pressure, AverageHumidity);
                        else sum1 = sum1 + CalculateGroupRefractiveIndex(DUTWavelength, row2, Pressure, AverageHumidity);
                        valid_results++;
                    }
                }
                else
                {
                    //ug(ProcNameMeasurement.EDM_BEAM_TEMPERATURE, "A PRT has a temperature reading which is not in the range 15 to 25 degree C", !beam_temperature_error_reported);
                    beam_temperature_error_reported = true;
                }
            }
            if (valid_results == total_prts_in_row) beam_temperature_error_reported = false;
            //compute the average
            double third_row = sum1 / valid_results;

            return third_row;
        }
        private static double fourthBeamAverage(ref int valid_results, int total_prts_in_row, ref MeasurementUpdateGui ug)
        {
            double sum1 = 0.0;
            fold_four_prts = new int[total_prts_in_row];
            for (int i = 0; i < total_prts_in_row; i++)
            {
                double row2 = temperatures[prtmap_over_walkway[i]];
                fold_four_prts[i] = prtmap_over_walkway[i];
                if (row2 > 15.0 && row2 < 25.0)
                {
                    sum1 = sum1 + row2;
                    valid_results++;
                }
                else
                {
                    //ug(ProcNameMeasurement.EDM_BEAM_TEMPERATURE, "A PRT has a temperature reading which is not in the range 15 to 25 degree C", !beam_temperature_error_reported);
                    beam_temperature_error_reported = true;
                }
            }

            if (valid_results == total_prts_in_row) beam_temperature_error_reported = false;

            //compute the average
            double fourth_row = sum1 / valid_results;

            return fourth_row;
        }

        private static double fourthAverageRI(ref int valid_results, int total_prts_in_row, ref MeasurementUpdateGui ug, bool is_laser, bool store)
        {
            double sum1 = 0.0;
            fold_four_prts = new int[total_prts_in_row];



            for (int i = 0; i < total_prts_in_row; i++)
            {
                double row2 = temperatures[prtmap_over_walkway[i]];
                fold_four_prts[i] = prtmap_over_walkway[i];
                if (row2 > 15.0 && row2 < 25.0)
                {

                    if (store)
                    {
                        if (i == 0)
                        {
                            Array.Clear(MeasurementPoint.group_temperatures4, 0, 30);
                            Array.Clear(MeasurementPoint.group_pressures4, 0, 30);
                            Array.Clear(MeasurementPoint.group_humidities4, 0, 30);
                        }

                        MeasurementPoint.group_temperatures4[prtmap_over_walkway[i]] = row2;  //store the temperature used for the RI calculation
                        MeasurementPoint.group_pressures4[prtmap_over_walkway[i]] = Pressure;
                        MeasurementPoint.group_humidities4[prtmap_over_walkway[i]] = AverageHumidity;
                        MeasurementPoint.group_wavelength = DUTWavelength;
                        if (is_laser) sum1 = sum1 + CalculatePhaseRefractiveIndex(MeasurementPoint.group_wavelength, MeasurementPoint.group_temperatures4[prtmap_over_walkway[i]], MeasurementPoint.group_pressures4[prtmap_over_walkway[i]], MeasurementPoint.group_humidities4[prtmap_over_walkway[i]]);
                        else sum1 = sum1 + CalculateGroupRefractiveIndex(MeasurementPoint.group_wavelength, MeasurementPoint.group_temperatures4[prtmap_over_walkway[i]], MeasurementPoint.group_pressures4[prtmap_over_walkway[i]], MeasurementPoint.group_humidities4[prtmap_over_walkway[i]]);


                    }
                    else
                    {
                        if (is_laser) sum1 = sum1 + CalculatePhaseRefractiveIndex(DUTWavelength, row2, Pressure, AverageHumidity);
                        else sum1 = sum1 + CalculateGroupRefractiveIndex(DUTWavelength, row2, Pressure, AverageHumidity);
                    }

                    valid_results++;
                }
                else
                {
                    //ug(ProcNameMeasurement.EDM_BEAM_TEMPERATURE, "A PRT has a temperature reading which is not in the range 15 to 25 degree C", !beam_temperature_error_reported);
                    beam_temperature_error_reported = true;
                }
            }

            if (valid_results == total_prts_in_row) beam_temperature_error_reported = false;

            //compute the average
            double fourth_row = sum1 / valid_results;

            return fourth_row;
        }

        /// <summary>
        /// Calculates the Phase refractive index based of the Ciddor Equation provided by the National Institute of Standards a Technology (NIST).
        /// Note - Temperature, Pressure and Humidity are input parameters to this equation as well, but these are continually updated within this class.
        /// </summary>
        /// <param name="lamda">The vacuum wavelength of the light source</param>
        public static double CalculatePhaseRefractiveIndex(double lamda)
        {
            const double alpha = 1.00062, beta = 3.14E-8, gamma = 5.6E-7;
            const double w0 = 295.235, w1 = 2.6422, w2 = -0.03238, w3 = 0.004028;
            const double k0 = 238.0185, k1 = 5792105, k2 = 57.362, k3 = 167917;
            const double a0 = 1.58123E-6, a1 = -2.9331E-8, a2 = 1.1043E-10;
            const double b0 = 5.707E-6, b1 = -2.051E-8;
            const double c0 = 1.9898E-4, c1 = -2.376E-6;
            const double d = 1.83E-11;
            const double e = -0.765E-8;
            const double p_R1 = 101325;
            const double T_R1 = 288.15;
            const double Z_a = 0.9995922115;
            const double Rho_vs = 0.00985938;
            const double R = 8.314472;
            const double M_v = 0.018015;

            double T_degC, T_K, P_Pa, H_RH, lambda_um, psv, f, xv, S, r_as, r_vs, M_a, r_axs, Z_m, rho_axs, rho_v, rho_a, n;

            P_Pa = Pressure * 100;
            T_degC = AverageLaserBeamTemperature;
            T_K = T_degC + 273.15;
            H_RH = AverageHumidity;
            lambda_um = lamda / 1000;

            f = alpha + beta * P_Pa + gamma * Math.Pow(T_degC, 2);
            psv = SVP(T_degC);
            xv = (H_RH / 100) * f * psv / P_Pa;

            S = 1 / Math.Pow(lambda_um, 2);

            r_as = 1E-8 * ((k1 / (k0 - S)) + (k3 / (k2 - S)));
            r_vs = 1.022 * 1E-8 * (w0 + w1 * S + w2 * Math.Pow(S, 2) + w3 * Math.Pow(S, 3));
            M_a = 0.0289635 + 1.2011 * 1E-8 * (CO2_Concentration * 1000000 - 400);
            r_axs = r_as * (1 + 5.34 * 1E-7 * (CO2_Concentration * 1000000 - 450));
            Z_m = 1 - (P_Pa / T_K) * (a0 + a1 * T_degC + a2 * Math.Pow(T_degC, 2) + (b0 + b1 * T_degC) * xv + (c0 + c1 * T_degC) * Math.Pow(xv, 2)) + Math.Pow((P_Pa / T_K), 2) * (d + e * Math.Pow(xv, 2));
            rho_axs = p_R1 * M_a / (Z_a * R * T_R1);
            rho_v = xv * P_Pa * M_v / (Z_m * R * T_K);
            rho_a = (1 - xv) * P_Pa * M_a / (Z_m * R * T_K);
            n = 1 + (rho_a / rho_axs) * r_axs + (rho_v / Rho_vs) * r_vs;
            return n;

        }

        /// <summary>
        /// Calculates the Phase refractive index based of the Ciddor Equation provided by the National Institute of Standards a Technology (NIST).
        /// Note - Temperature, Pressure and Humidity are input parameters to this equation as well, but these are continually updated within this class.
        /// </summary>
        /// <param name="lamda">The vacuum wavelength of the light source</param>
        /// <param name="t">The temperature of the beam path</param>
        /// <param name="p">The pressure</param>
        /// <param name="h">The humidity of the beam path</param>
        public static double CalculatePhaseRefractiveIndex(double lamda, double t, double p, double h)
        {
            const double alpha = 1.00062, beta = 3.14E-8, gamma = 5.6E-7;
            const double w0 = 295.235, w1 = 2.6422, w2 = -0.03238, w3 = 0.004028;
            const double k0 = 238.0185, k1 = 5792105, k2 = 57.362, k3 = 167917;
            const double a0 = 1.58123E-6, a1 = -2.9331E-8, a2 = 1.1043E-10;
            const double b0 = 5.707E-6, b1 = -2.051E-8;
            const double c0 = 1.9898E-4, c1 = -2.376E-6;
            const double d = 1.83E-11;
            const double e = -0.765E-8;
            const double p_R1 = 101325;
            const double T_R1 = 288.15;
            const double Z_a = 0.9995922115;
            const double Rho_vs = 0.00985938;
            const double R = 8.314472;
            const double M_v = 0.018015;

            double T_degC, T_K, P_Pa, H_RH, lambda_um, psv, f, xv, S, r_as, r_vs, M_a, r_axs, Z_m, rho_axs, rho_v, rho_a, n;


            P_Pa = p * 100;
            T_degC = t;
            T_K = T_degC + 273.15;
            H_RH = h;
            lambda_um = lamda / 1000;

            f = alpha + beta * P_Pa + gamma * Math.Pow(T_degC, 2);
            psv = SVP(T_degC);
            xv = (H_RH / 100) * f * psv / P_Pa;

            S = 1 / Math.Pow(lambda_um, 2);

            r_as = 1E-8 * ((k1 / (k0 - S)) + (k3 / (k2 - S)));
            r_vs = 1.022 * 1E-8 * (w0 + w1 * S + w2 * Math.Pow(S, 2) + w3 * Math.Pow(S, 3));
            M_a = 0.0289635 + 1.2011 * 1E-8 * (CO2_Concentration * 1000000 - 400);
            r_axs = r_as * (1 + 5.34 * 1E-7 * (CO2_Concentration * 1000000 - 450));
            Z_m = 1 - (P_Pa / T_K) * (a0 + a1 * T_degC + a2 * Math.Pow(T_degC, 2) + (b0 + b1 * T_degC) * xv + (c0 + c1 * T_degC) * Math.Pow(xv, 2)) + Math.Pow((P_Pa / T_K), 2) * (d + e * Math.Pow(xv, 2));
            rho_axs = p_R1 * M_a / (Z_a * R * T_R1);
            rho_v = xv * P_Pa * M_v / (Z_m * R * T_K);
            rho_a = (1 - xv) * P_Pa * M_a / (Z_m * R * T_K);
            n = 1 + (rho_a / rho_axs) * r_axs + (rho_v / Rho_vs) * r_vs;
            return n;

        }

        /// <summary>
        /// Calculates the saturation vapour pressure of water (provided by the National Institute of Standards a Technology (NIST)).
        /// see http://emtoolbox.nist.gov/Wavelength/Documentation.asp
        /// Note - Temperature, Pressure and Humidity are input parameters to this equation as well, but these are continually updated within this class.
        /// </summary>
        /// <param name="t">The temperature of the air</param>
        public static double SVP(double t)
        {

            const double k1 = 1167.05214528, k2 = -724213.167032, k3 = -17.0738469401, K4 = 12020.8247025, K5 = -3232555.03223, K6 = 14.9151086135, K7 = -4823.26573616, K8 = 405113.405421, K9 = -0.238555575678, K10 = 650.175348448;
            double T_degC, T_K, Omega, A, B, C, X;

            //assumes T > 0
            T_degC = t;

            T_K = T_degC + 273.15;
            Omega = T_K + K9 / (T_K - K10);
            A = Math.Pow(Omega, 2) + k1 * Omega + k2;
            B = k3 * Math.Pow(Omega, 2) + K4 * Omega + K5;
            C = K6 * Math.Pow(Omega, 2) + K7 * Omega + K8;
            X = -B + Math.Sqrt(Math.Pow(B, 2) - 4 * A * C);
            return 1000000 * Math.Pow((2 * C) / X, 4);
        }

        /// <summary>
        /// Calculates the group refractive index (see NIST paper - Refractive index of air: new equations for the visible and near infrared. Philip E. Ciddor 1996).
        /// Note - Temperature, Pressure and Humidity are input parameters to this equation as well, but these are continually updated within this class.
        /// </summary>
        /// <param name="lambda">The vacuum wavelength of the source</param>
        public static double CalculateGroupRefractiveIndex(double lamda)
        {

            const double alpha = 1.00062, beta = 3.14E-8, gamma = 5.6E-7;
            const double w0 = 295.235, w1 = 2.6422, w2 = -0.03238, w3 = 0.004028;
            const double k0 = 238.0185, k1 = 5792105, k2 = 57.362, k3 = 167917;
            const double a0 = 1.58123E-6, a1 = -2.9331E-8, a2 = 1.1043E-10;
            const double b0 = 5.707E-6, b1 = -2.051E-8;
            const double c0 = 1.9898E-4, c1 = -2.376E-6;
            const double d = 1.83E-11;
            const double e = -0.765E-8;
            const double p_R1 = 101325;
            const double T_R1 = 288.15;
            const double Z_a = 0.9995922115;
            const double Rho_vs = 0.00985938;
            const double R = 8.314472;
            const double M_v = 0.018015;

            double T_degC, T_K, P_Pa, H_RH, lambda_um, psv, f, xv, S, r_as, r_vs, M_a, r_axs, Z_m, rho_axs, rho_v, rho_a, n;
            P_Pa = Pressure * 100;

            T_degC = AverageEDMBeamTemperature;
            T_K = T_degC + 273.15;
            H_RH = AverageHumidity;
            lambda_um = lamda / 1000;

            f = alpha + beta * P_Pa + gamma * Math.Pow(T_degC, 2);
            psv = SVP(T_degC);
            xv = (H_RH / 100) * f * psv / P_Pa;

            S = 1 / Math.Pow(lambda_um, 2);

            //equation 1
            r_as = 1E-8 * ((k1 / (k0 - S)) + (k3 / (k2 - S)));

            //equation 11

            r_vs = 1E-8 * 1.022 * (w0 + (3 * w1 * S) + (5 * w2 * Math.Pow(S, 2)) + (7 * w3 * Math.Pow(S, 3)));
            //double r_vs_2 = 1.022 * 1E-8 * (w0 + w1 * S + w2 * Math.Pow(S, 2) + w3 * Math.Pow(S, 3));

            M_a = 0.0289635 + 1.2011 * 1E-8 * (CO2_Concentration * 1000000 - 400);

            //equation 2
            //double r_axs2 = r_as * (1 + 5.34 * 1E-7 * (CO2_Concentration - 450));

            //equation 10

            double n1, n2, d1, d2, r_axs_prime;
            n1 = k1 * (k0 + S);
            d1 = Math.Pow((k0 - S), 2);
            n2 = k3 * (k2 + S);
            d2 = Math.Pow(k2 - S, 2);
            r_axs_prime = (1 + 5.34 * 1E-7 * (CO2_Concentration * 1000000 - 450));
            r_axs = 1E-8 * ((n1 / d1) + (n2 / d2)) * r_axs_prime;

            Z_m = 1 - (P_Pa / T_K) * (a0 + a1 * T_degC + a2 * Math.Pow(T_degC, 2) + (b0 + b1 * T_degC) * xv + (c0 + c1 * T_degC) * Math.Pow(xv, 2)) + Math.Pow((P_Pa / T_K), 2) * (d + e * Math.Pow(xv, 2));
            rho_axs = p_R1 * M_a / (Z_a * R * T_R1);
            rho_v = xv * P_Pa * M_v / (Z_m * R * T_K);
            rho_a = (1 - xv) * P_Pa * M_a / (Z_m * R * T_K);
            n = 1 + (rho_a / rho_axs) * r_axs + (rho_v / Rho_vs) * r_vs;
            return n;
        }

        /// <summary>
        /// Calculates the group refractive index (see NIST paper - Refractive index of air: new equations for the visible and near infrared. Philip E. Ciddor 1996).
        /// Note - Temperature, Pressure and Humidity are input parameters to this equation as well, but these are continually updated within this class.
        /// </summary>
        /// <param name="lambda">The vacuum wavelength of the source</param>
        /// <param name="t">temperature</param>
        /// <param name="p">pressure</param>
        /// <param name="h">pressure</param>
        public static double CalculateGroupRefractiveIndex(double lamda, double t, double p, double h)
        {

            const double alpha = 1.00062, beta = 3.14E-8, gamma = 5.6E-7;
            const double w0 = 295.235, w1 = 2.6422, w2 = -0.03238, w3 = 0.004028;
            const double k0 = 238.0185, k1 = 5792105, k2 = 57.362, k3 = 167917;
            const double a0 = 1.58123E-6, a1 = -2.9331E-8, a2 = 1.1043E-10;
            const double b0 = 5.707E-6, b1 = -2.051E-8;
            const double c0 = 1.9898E-4, c1 = -2.376E-6;
            const double d = 1.83E-11;
            const double e = -0.765E-8;
            const double p_R1 = 101325;
            const double T_R1 = 288.15;
            const double Z_a = 0.9995922115;
            const double Rho_vs = 0.00985938;
            const double R = 8.314472;
            const double M_v = 0.018015;

            double T_degC, T_K, P_Pa, H_RH, lambda_um, psv, f, xv, S, r_as, r_vs, M_a, r_axs, Z_m, rho_axs, rho_v, rho_a, n;
            P_Pa = p * 100;

            T_degC = t;
            T_K = T_degC + 273.15;

            H_RH = h;
            lambda_um = lamda / 1000;

            f = alpha + beta * P_Pa + gamma * Math.Pow(T_degC, 2);
            psv = SVP(T_degC);
            xv = (H_RH / 100) * f * psv / P_Pa;

            S = 1 / Math.Pow(lambda_um, 2);

            //equation 1
            r_as = 1E-8 * ((k1 / (k0 - S)) + (k3 / (k2 - S)));

            //equation 11

            r_vs = 1E-8 * 1.022 * (w0 + (3 * w1 * S) + (5 * w2 * Math.Pow(S, 2)) + (7 * w3 * Math.Pow(S, 3)));
            //double r_vs_2 = 1.022 * 1E-8 * (w0 + w1 * S + w2 * Math.Pow(S, 2) + w3 * Math.Pow(S, 3));

            M_a = 0.0289635 + 1.2011 * 1E-8 * (CO2_Concentration * 1000000 - 400);

            //equation 2
            //double r_axs2 = r_as * (1 + 5.34 * 1E-7 * (CO2_Concentration - 450));

            //equation 10

            double n1, n2, d1, d2, r_axs_prime;
            n1 = k1 * (k0 + S);
            d1 = Math.Pow((k0 - S), 2);
            n2 = k3 * (k2 + S);
            d2 = Math.Pow(k2 - S, 2);
            r_axs_prime = (1 + 5.34 * 1E-7 * (CO2_Concentration * 1000000 - 450));
            r_axs = 1E-8 * ((n1 / d1) + (n2 / d2)) * r_axs_prime;

            Z_m = 1 - (P_Pa / T_K) * (a0 + a1 * T_degC + a2 * Math.Pow(T_degC, 2) + (b0 + b1 * T_degC) * xv + (c0 + c1 * T_degC) * Math.Pow(xv, 2)) + Math.Pow((P_Pa / T_K), 2) * (d + e * Math.Pow(xv, 2));
            rho_axs = p_R1 * M_a / (Z_a * R * T_R1);
            rho_v = xv * P_Pa * M_v / (Z_m * R * T_K);
            rho_a = (1 - xv) * P_Pa * M_a / (Z_m * R * T_K);
            n = 1 + (rho_a / rho_axs) * r_axs + (rho_v / Rho_vs) * r_vs;
            return n;
        }

        public void SetDeviceType(short device)
        {
            d_type = device;
            switch (device)
            {
                case Device.EDM:
                    survey_device = new EDM(ref dutug);
                    break;
                case Device.TOTAL_STATION:
                    survey_device = new Total_Station(ref dutug);
                    break;
                case Device.SECOND_LASER:
                    survey_device = new Laser_Aux(ref dutug);
                    break;
                default:
                    break;
            }
        }

        public void doDrawPrep()
        {
            //only one thread at a time please
            lock (lockthis)
            {
                Thread.Sleep(20);
                double avg_temp = Math.Round(Measurement.AverageTemperature, 2);
                double avg_temp2 = Math.Round(Measurement.AverageLaserBeamTemperature, 2);
                double avg_temp3 = Math.Round(Measurement.AverageEDMBeamTemperature, 2);
                double AverRH = Math.Round(Measurement.AverageHumidity, 2);
                double RH1_Corr = Math.Round(ref_logger1.Correction, 2);
                double RH2_Corr = Math.Round(ref_logger2.Correction, 2);
                StringBuilder text = new StringBuilder();


                try
                {
                    text.AppendLine("WaveLength (as read from HP5519 Laser head): " + reflaser.Wavelength.ToString() + " nm");
                    text.AppendLine("DUT WaveLength: " + DUTWavelength.ToString() + " nm");
                    text.AppendLine("Average Tunnel Air Temp: " + avg_temp.ToString() + " °C");
                    text.AppendLine("Average Laser Beam Temp: " + avg_temp2.ToString() + " °C");
                    text.AppendLine("Average EDM Beam Temp: " + avg_temp3.ToString() + " °C");
                    text.AppendLine("Air Pres: " + Pressure.ToString() + " hPa");
                    text.AppendLine("Air Pres Correction: " + ref_barometer.Correction.ToString() + " hPa");
                    text.AppendLine("Average %RH: " + AverRH.ToString() + "%");
                    text.AppendLine("Humidity Logger 1 Correction: " + RH1_Corr.ToString() + "%");
                    text.AppendLine("Humidity Logger 2 Correction: " + RH2_Corr.ToString() + "%");
                    text.AppendLine("Agilent RI Corr: " + reflaser.AirCompensation.ToString());

                    if (block != true)
                    {
                        text.AppendLine("Phase Refractive Index (Laser): " + CalculateAverageRILaserBeam(ref reflaser, LaserWavelength, false).ToString());

                        try
                        {
                            if (d_type == Device.SECOND_LASER)
                            {
                                text.AppendLine("Phase Refractive Index (second laser) :" + calculateAverageRIEDMBeam(DUT.Beamfolds, ref reflaser, ref mug, DUTWavelength, true, false));
                            }
                            else
                            {
                                text.AppendLine("Group Refractive Index (EDM Device) : " + calculateAverageRIEDMBeam(DUT.Beamfolds, ref reflaser, ref mug, DUTWavelength, false, false));
                            }
                        }
                        catch (FormatException)
                        {
                            if (d_type == Device.SECOND_LASER)
                            {
                                text.AppendLine("Phase Refractive Index (" + d_type.ToString() + ") : " + calculateAverageRIEDMBeam(DUT.Beamfolds, ref reflaser, ref mug, 850, true, false));
                            }
                            else
                            {
                                text.AppendLine("Group Refractive Index (" + d_type.ToString() + ") : " + calculateAverageRIEDMBeam(DUT.Beamfolds, ref reflaser, ref mug, 850, false, false));
                            }
                        }
                    }
                    text.AppendLine("CO2 Level: " + CO2.ToString() + "\n");

                    int[] a = LaserPRTSUsed;
                    if (a != null)
                    {
                        string laser_prts = "";
                        for (int i = 0; i < a.Length; i++)
                        {
                            laser_prts = string.Concat(laser_prts, (a[i] + 1).ToString() + ",");
                        }
                        text.AppendLine("Laser PRTs Used: " + laser_prts.ToString());

                        switch (DUT.Beamfolds)
                        {
                            case 0:
                                int[] b = Row1PRTSUsed;
                                string one_beam_prts = "";
                                for (int i = 0; i < b.Length; i++)
                                {
                                    one_beam_prts = string.Concat(one_beam_prts, (b[i] + 1).ToString() + ",");
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
                        text.AppendLine("Temperature PRT " + (i + 1).ToString() + ": " + Measurement.getTemperature(i).ToString());
                    }
                    PrintString = text.ToString(); ;
                }
                catch (ObjectDisposedException)
                {
                    Application.Exit();
                    PrintString = "";
                }
            }
        }
        public void setIntermediateValue(double[] values, int index)
        {
            try
            {
                intermediate_values[index] = values;
            }
            catch (IndexOutOfRangeException)
            {
                Array.Resize<double[]>(ref intermediate_values, index + 1);
                intermediate_values[index] = values;
            }
        }
        public static void Measure(object stateinfo)
        {
            Measurement asyc_meas = (Measurement)stateinfo;
            int i = 0;
            while (MeasurementLoopEnabled)
            {
                asyc_meas.measurement_thread.Join(100);
                //Thread.Sleep(10);  //Don't thrash the thread!
                //if we need a one off measurement
                if (Measurement.do_one_off)
                {
                    short exstg = Measurement.current_execution_stage;
                    Measurement.current_execution_stage = ExecutionStage.ONE_OFF;
                    //Try and connect if need be
                    if (DUT.TryConnect())
                    {

                        //read the laser and read the DUT.
                        if (!DUT_Request(asyc_meas, -1, false))
                        {
                            asyc_meas.mug(ProcNameMeasurement.NETWORK_TRANSMISSION, "Cannot connect to the device under test - Check connections, hostname", true);
                        }

                        //return the execution stage to how it was before
                        Measurement.CurrentExecutionStage = exstg;

                        //done the one off measurement so stop
                        Measurement.do_one_off = false;
                    }
                }
                if (Measurement.one_off_reset)
                {
                    short exstg = Measurement.current_execution_stage;
                    Measurement.current_execution_stage = ExecutionStage.RESET;
                    //Try and connect if need be
                    if (DUT.TryConnect())
                    {

                        //reset the DUT.
                        if (!DUT_Request(asyc_meas, -1, true))
                        {
                            asyc_meas.mug(ProcNameMeasurement.NETWORK_TRANSMISSION, "Cannot connect to the device under test - Check connections, hostname", true);
                        }

                        //return the execution stage to how it was before
                        Measurement.CurrentExecutionStage = exstg;

                        //done the one off measurement so stop
                        Measurement.one_off_reset = false;
                    }
                }
                switch (Measurement.CurrentExecutionStage)
                {

                    case ExecutionStage.START:
                        Thread.Sleep(500);

                        //Try and connect if need be
                        if (DUT.TryConnect())
                        {
                            //reset the laser
                            if (asyc_meas.DoReset) asyc_meas.reflaser.Reset();

                            if (asyc_meas.AbortMeasurement == true)
                            {
                                Measurement.CurrentExecutionStage = ExecutionStage.IDLE;
                                break;
                            }
                            //read the laser and reset and read the edm.
                            DUT_Request(asyc_meas, -1, true);
                        }
                        else asyc_meas.mug(ProcNameMeasurement.NETWORK_TRANSMISSION, "Couldn't connect to the device under test, check network connections", true);
                        break;
                    case ExecutionStage.INTERDIATE:

                        //Try and connect if need be
                        if (DUT.TryConnect())
                        {
                            while ((i < asyc_meas.Intermediate.Length) && MeasurementLoopEnabled)
                            {
                                //get the target.
                                double target = asyc_meas.Intermediate[i];
                                Measurement.Target = target;
                                //read the target and the laser to detemine if we need to move
                                MoveToTargetWindow(asyc_meas, target);

                                if (asyc_meas.AbortMeasurement == true)
                                {
                                    Measurement.CurrentExecutionStage = ExecutionStage.IDLE;
                                    break;
                                }

                                //if we can't do a read of the edm and laser the trolley is not allowed to move so wait 
                                while (MeasurementLoopEnabled)
                                {
                                    if (DUT_Request(asyc_meas, i, false)) break;
                                }
                                i++;
                            }
                        }
                        break;
                    case ExecutionStage.END:

                        //Try and connect if need be
                        if (DUT.TryConnect())
                        {

                            //get the target.
                            double target_end = asyc_meas.EndPos;
                            Measurement.Target = target_end;

                            MoveToTargetWindow(asyc_meas, target_end);


                            if (asyc_meas.AbortMeasurement == true)
                            {
                                Measurement.current_execution_stage = ExecutionStage.IDLE;
                                break;
                            }

                            //read the edm and laser
                            DUT_Request(asyc_meas, -1, false);
                        }
                        break;
                    case ExecutionStage.IDLE:
                        Thread.Sleep(200);
                        break;
                }
            }
        }

        public static bool MoveToTargetWindow(Measurement current_meas, double target)
        {
            Measurement asyc_meas = current_meas;
            bool not_in_target_window = true;

            //read the target and the laser to detemine which way we need to move
            while (not_in_target_window)
            {
                if ((asyc_meas.reflaser.R_Sample < (target - 0.075)) && MeasurementLoopEnabled)
                {
                    //no point is executing move command if we can't talk to the trolley!
                    if (asyc_meas.reftrolley.isPortOpen)
                    {

                        //we need to move the trolley in reverse
                        //asyc_meas.reftrolley.ProcToDo = ProcNameTrolley.STOP;
                        asyc_meas.mug(ProcNameMeasurement.TROLLEY_SET, "STOP", false);
                        asyc_meas.measurement_thread.Join(100);
                        //byte[] sb = new byte[1];
                        //sb[0] = 160;
                        //asyc_meas.reftrolley.SpeedByte = sb;
                        //asyc_meas.reftrolley.ProcToDo = ProcNameTrolley.SETSPEED;
                        asyc_meas.mug(ProcNameMeasurement.TROLLEY_SET, "SPEED160", false);
                        asyc_meas.measurement_thread.Join(100);
                        //asyc_meas.reftrolley.ProcToDo = ProcNameTrolley.REVERSE;
                        asyc_meas.mug(ProcNameMeasurement.TROLLEY_SET, "REVERSE", false);
                        asyc_meas.measurement_thread.Join(100);
                        //asyc_meas.reftrolley.ProcToDo = ProcNameTrolley.GO;
                        asyc_meas.mug(ProcNameMeasurement.TROLLEY_SET, "GO", false);
                        asyc_meas.measurement_thread.Join(100);

                        //we're too close to ramp the speed up so just move slowly to target
                        if ((asyc_meas.reflaser.R_Sample) > target - 0.5)
                        {
                            //sb[0] = 160;
                            //asyc_meas.reftrolley.SpeedByte = sb;
                            //asyc_meas.reftrolley.ProcToDo = ProcNameTrolley.SETSPEED;
                            asyc_meas.mug(ProcNameMeasurement.TROLLEY_SET, "SPEED100", false);
                        }
                        else
                        {
                            //we're far enough away to ramp the speed up.
                            //sb[0] = 150;
                            //asyc_meas.reftrolley.SpeedByte = sb;
                            //asyc_meas.reftrolley.ProcToDo = ProcNameTrolley.SETSPEED;
                            asyc_meas.mug(ProcNameMeasurement.TROLLEY_SET, "SPEED150", false);
                            asyc_meas.measurement_thread.Join(300);
                            //sb[0] = 110;
                            //asyc_meas.reftrolley.SpeedByte = sb;
                            //asyc_meas.reftrolley.ProcToDo = ProcNameTrolley.SETSPEED;
                            asyc_meas.mug(ProcNameMeasurement.TROLLEY_SET, "SPEED110", false);
                            asyc_meas.measurement_thread.Join(300);
                            //sb[0] = 80;
                            //asyc_meas.reftrolley.SpeedByte = sb;
                            //asyc_meas.reftrolley.ProcToDo = ProcNameTrolley.SETSPEED;
                            asyc_meas.mug(ProcNameMeasurement.TROLLEY_SET, "SPEED80", false);
                            asyc_meas.measurement_thread.Join(300);
                            //sb[0] = 70;
                            //asyc_meas.reftrolley.SpeedByte = sb;
                            //asyc_meas.reftrolley.ProcToDo = ProcNameTrolley.SETSPEED;
                            asyc_meas.mug(ProcNameMeasurement.TROLLEY_SET, "SPEED70", false);
                            asyc_meas.measurement_thread.Join(300);
                            //sb[0] = 50;
                            //asyc_meas.reftrolley.SpeedByte = sb;
                            //asyc_meas.reftrolley.ProcToDo = ProcNameTrolley.SETSPEED;
                            asyc_meas.mug(ProcNameMeasurement.TROLLEY_SET, "SPEED50", false);
                            asyc_meas.measurement_thread.Join(300);
                            //sb[0] = 40;
                            //asyc_meas.reftrolley.SpeedByte = sb;
                            //asyc_meas.reftrolley.ProcToDo = ProcNameTrolley.SETSPEED;
                            asyc_meas.mug(ProcNameMeasurement.TROLLEY_SET, "SPEED40", false);
                            asyc_meas.measurement_thread.Join(300);
                            //sb[0] = 30;
                            //asyc_meas.reftrolley.SpeedByte = sb;
                            //asyc_meas.reftrolley.ProcToDo = ProcNameTrolley.SETSPEED;
                            asyc_meas.mug(ProcNameMeasurement.TROLLEY_SET, "SPEED30", false);
                            asyc_meas.measurement_thread.Join(300);
                        }

                        bool done1 = false;
                        bool done2 = false;
                        bool done3 = false;

                        //wait until we reach the target. or until we are told to stop
                        while ((asyc_meas.reflaser.R_Sample < target) && MeasurementLoopEnabled && asyc_meas.reftrolley.isPortOpen)
                        {
                            asyc_meas.measurement_thread.Join(10);
                            //make sure we are moving in the correct direction
                            //asyc_meas.reftrolley.ProcToDo = ProcNameTrolley.REVERSE;
                            asyc_meas.mug(ProcNameMeasurement.TROLLEY_SET, "REVERSE", false);
                            asyc_meas.measurement_thread.Join(10);
                            //asyc_meas.reftrolley.ProcToDo = ProcNameTrolley.GO;
                            asyc_meas.mug(ProcNameMeasurement.TROLLEY_SET, "GO", false);


                            if (((asyc_meas.reflaser.R_Sample) > target - 0.025) & !done1)
                            {
                                //sb[0] = 155;
                                //asyc_meas.reftrolley.SpeedByte = sb;
                                //asyc_meas.reftrolley.ProcToDo = ProcNameTrolley.SETSPEED;
                                asyc_meas.mug(ProcNameMeasurement.TROLLEY_SET, "SPEED200", false);
                                done1 = true;
                                done2 = true;
                                done3 = true;
                            }

                            if (((asyc_meas.reflaser.R_Sample) > target - 0.05) & !done2)
                            {
                                //sb[0] = 130;
                                //asyc_meas.reftrolley.SpeedByte = sb;
                                //asyc_meas.reftrolley.ProcToDo = ProcNameTrolley.SETSPEED;
                                asyc_meas.mug(ProcNameMeasurement.TROLLEY_SET, "SPEED130", false);
                                done2 = true;
                                done3 = true;
                            }

                            if (((asyc_meas.reflaser.R_Sample) > target - 0.2) & !done3)
                            {
                                //sb[0] = 100;
                                //asyc_meas.reftrolley.SpeedByte = sb;
                                //asyc_meas.reftrolley.ProcToDo = ProcNameTrolley.SETSPEED;
                                asyc_meas.mug(ProcNameMeasurement.TROLLEY_SET, "SPEED100", false);
                                done3 = true;
                            }

                            if (asyc_meas.AbortMeasurement == true)
                            {
                                break;
                            }
                        }
                    }
                }
                else if ((asyc_meas.reflaser.R_Sample > (target + 0.075)) && MeasurementLoopEnabled)
                {

                    //no point is executing move command if we can't talk to the trolley!
                    if (asyc_meas.reftrolley.isPortOpen)
                    {
                        //we need to move the trolley forward
                        //asyc_meas.reftrolley.ProcToDo = ProcNameTrolley.STOP;
                        asyc_meas.mug(ProcNameMeasurement.TROLLEY_SET, "STOP", false);
                        asyc_meas.measurement_thread.Join(100);
                        //byte[] sb = new byte[1];
                        //sb[0] = 160;
                        //asyc_meas.reftrolley.SpeedByte = sb;
                        //asyc_meas.reftrolley.ProcToDo = ProcNameTrolley.SETSPEED;
                        asyc_meas.mug(ProcNameMeasurement.TROLLEY_SET, "SPEED160", false);
                        asyc_meas.measurement_thread.Join(100);
                        //asyc_meas.reftrolley.ProcToDo = ProcNameTrolley.FORWARD;
                        asyc_meas.mug(ProcNameMeasurement.TROLLEY_SET, "FORWARD", false);
                        asyc_meas.measurement_thread.Join(100);
                        //asyc_meas.reftrolley.ProcToDo = ProcNameTrolley.GO;
                        asyc_meas.mug(ProcNameMeasurement.TROLLEY_SET, "GO", false);
                        asyc_meas.measurement_thread.Join(100);



                        if ((asyc_meas.reflaser.R_Sample) < target + 0.50)
                        {
                            //sb[0] = 160;
                            //asyc_meas.reftrolley.SpeedByte = sb;
                            //asyc_meas.reftrolley.ProcToDo = ProcNameTrolley.SETSPEED;
                            asyc_meas.mug(ProcNameMeasurement.TROLLEY_SET, "SPEED100", false);
                        }
                        else
                        {
                            //sb[0] = 150;
                            //asyc_meas.reftrolley.SpeedByte = sb;
                            //asyc_meas.reftrolley.ProcToDo = ProcNameTrolley.SETSPEED;
                            asyc_meas.mug(ProcNameMeasurement.TROLLEY_SET, "SPEED150", false);
                            asyc_meas.measurement_thread.Join(300);
                            //sb[0] = 110;
                            //asyc_meas.reftrolley.SpeedByte = sb;
                            //asyc_meas.reftrolley.ProcToDo = ProcNameTrolley.SETSPEED;
                            asyc_meas.mug(ProcNameMeasurement.TROLLEY_SET, "SPEED110", false);
                            asyc_meas.measurement_thread.Join(300);
                            //sb[0] = 80;
                            //asyc_meas.reftrolley.SpeedByte = sb;
                            //asyc_meas.reftrolley.ProcToDo = ProcNameTrolley.SETSPEED;
                            asyc_meas.mug(ProcNameMeasurement.TROLLEY_SET, "SPEED80", false);
                            asyc_meas.measurement_thread.Join(300);
                            //sb[0] = 50;
                            //asyc_meas.reftrolley.SpeedByte = sb;
                            //asyc_meas.reftrolley.ProcToDo = ProcNameTrolley.SETSPEED;
                            asyc_meas.mug(ProcNameMeasurement.TROLLEY_SET, "SPEED50", false);
                            asyc_meas.measurement_thread.Join(300); ;
                            //sb[0] = 40;
                            //asyc_meas.reftrolley.SpeedByte = sb;
                            //asyc_meas.reftrolley.ProcToDo = ProcNameTrolley.SETSPEED;
                            asyc_meas.mug(ProcNameMeasurement.TROLLEY_SET, "SPEED40", false);
                            asyc_meas.measurement_thread.Join(300);
                            //sb[0] = 30;
                            //asyc_meas.reftrolley.SpeedByte = sb;
                            //asyc_meas.reftrolley.ProcToDo = ProcNameTrolley.SETSPEED;
                            asyc_meas.mug(ProcNameMeasurement.TROLLEY_SET, "SPEED30", false);
                            asyc_meas.measurement_thread.Join(300);
                            //sb[0] = 20;
                            //asyc_meas.reftrolley.SpeedByte = sb;
                            //asyc_meas.reftrolley.ProcToDo = ProcNameTrolley.SETSPEED;
                            asyc_meas.mug(ProcNameMeasurement.TROLLEY_SET, "SPEED20", false);
                            asyc_meas.measurement_thread.Join(300);
                        }


                        bool done1 = false;
                        bool done2 = false;
                        bool done3 = false;

                        //wait until we reach the target. or until we are told to stop
                        while ((asyc_meas.reflaser.R_Sample > target) && MeasurementLoopEnabled)
                        {
                            asyc_meas.measurement_thread.Join(10);
                            //make sure we are moving in the correct direction
                            //asyc_meas.reftrolley.ProcToDo = ProcNameTrolley.FORWARD;
                            asyc_meas.mug(ProcNameMeasurement.TROLLEY_SET, "FORWARD", false);
                            asyc_meas.measurement_thread.Join(10);
                            //asyc_meas.reftrolley.ProcToDo = ProcNameTrolley.GO;
                            asyc_meas.mug(ProcNameMeasurement.TROLLEY_SET, "GO", false);

                            if (((asyc_meas.reflaser.R_Sample) < target + 0.025) & !done1)
                            {
                                //sb[0] = 155;
                                //asyc_meas.reftrolley.SpeedByte = sb;
                                //asyc_meas.reftrolley.ProcToDo = ProcNameTrolley.SETSPEED;
                                asyc_meas.mug(ProcNameMeasurement.TROLLEY_SET, "SPEED200", false);
                                done1 = true;
                                done2 = true;
                                done3 = true;
                            }

                            if (((asyc_meas.reflaser.R_Sample) < target + 0.05) & !done2)
                            {
                                //sb[0] = 130;
                                // asyc_meas.reftrolley.SpeedByte = sb;
                                //asyc_meas.reftrolley.ProcToDo = ProcNameTrolley.SETSPEED;
                                asyc_meas.mug(ProcNameMeasurement.TROLLEY_SET, "SPEED130", false);
                                done2 = true;
                                done3 = true;
                            }

                            if (((asyc_meas.reflaser.R_Sample) < target + 0.2) & !done3)
                            {
                                //sb[0] = 100;
                                //asyc_meas.reftrolley.SpeedByte = sb;
                                //asyc_meas.reftrolley.ProcToDo = ProcNameTrolley.SETSPEED;
                                asyc_meas.mug(ProcNameMeasurement.TROLLEY_SET, "SPEED100", false);
                                done3 = true;
                            }

                            if (asyc_meas.AbortMeasurement == true)
                            {
                                break;
                            }
                        }
                    }
                }

                else
                {
                    //if we get to here we are in the target window.  But we still need to account for a possible runaway trolley situation.

                    //stop the trolley immediately
                    //asyc_meas.reftrolley.ProcToDo = ProcNameTrolley.STOP;
                    asyc_meas.mug(ProcNameMeasurement.TROLLEY_SET, "STOP", false);

                    bool trolley_stationary = false;
                    while (!trolley_stationary)
                    {
                        //stop the trolley
                        double reading1 = asyc_meas.reflaser.R_Sample;
                        asyc_meas.measurement_thread.Join(100);
                        double reading2 = asyc_meas.reflaser.R_Sample;

                        if (reading1 == reading2) continue; //if readings are identical we assume the laser hasn't got an updated reading
                        else if (Math.Abs(reading1 - reading2) > 0.00001)
                        {
                            //asyc_meas.reftrolley.ProcToDo = ProcNameTrolley.STOP;
                            asyc_meas.mug(ProcNameMeasurement.TROLLEY_SET, "SPEED255", false);
                            asyc_meas.mug(ProcNameMeasurement.TROLLEY_SET, "STOP", false);
                        }
                        else trolley_stationary = true;
                    }

                    //check if we are still in the target window.
                    if ((asyc_meas.reflaser.R_Sample <= (target + 0.075)) && (asyc_meas.reflaser.R_Sample >= (target - 0.075)))
                    {
                        //we're in the target window and the trolley is stationary, so we're done
                        not_in_target_window = false;
                    }
                }
            }
            //dwell for the given dwell time
            asyc_meas.measurement_thread.Join((int)asyc_meas.DTime * 1000);
            return true;
        }
        public static bool DUT_Request(Measurement current_meas, int array_index, bool reset)
        {

            Measurement asyc_meas = current_meas;
            //bool on_iteration = false;

            string device_type = DUT.deviceType;

            if (DUT.Connected())
            {
                if (Measurement.measurement_status)
                {

                    if (current_execution_stage == ExecutionStage.RESET)
                    {
                        string result_ = "";
                        if (!survey_device.Request("Reset", ref result_))
                        {
                            //invoke the gui to report a send/recevice error.
                            asyc_meas.mug(ProcNameMeasurement.NETWORK_TRANSMISSION, MeasurementError.EDM_SEND_RECEIVE_ERROR, true);
                            return false;
                        }
                        else
                        {
                            if (!result_.Equals("true"))
                            {
                                //invoke the gui to report a data conversion error.
                                asyc_meas.mug(ProcNameMeasurement.DATA_CONVERSION, MeasurementError.EDM_FORMAT_ERROR, true);
                                return false;
                            }
                        }
                        return true;
                    }

                    //array to hold the values we measure 0 = laser raw reading, 1 = RI correction laser, 2 = Laser with MSL RI Correction applied ,
                    //                                    3 = DUT raw reading, 4 = DUT with default correction removed, 5 = DUT with MSL RI Correction applied ,  
                    //                                    6 = DUT RI Correction, 7 = DUT standard deviation, 8 = DUT averaging  
                    //                                    9 = Laser Beam Temperature, 10 = DUT beam Temperature, 11,pressure used for phase RI, 12 Pressure used for group RI
                    //      

                    //wait here for the dwell time
                    asyc_meas.measurement_thread.Join((int)asyc_meas.dwell_time * 1000);
                    //Thread.Sleep((int) asyc_meas.dwell_time*1000);                              

                    double[] vals = new double[18];
                    //Record a reading of the laser.
                    vals[0] = asyc_meas.reflaser.R_Sample;

                    block = true;
                    //Refractive index correction for the laser
                    vals[1] = CalculateAverageRILaserBeam(ref asyc_meas.reflaser, asyc_meas.reflaser.Wavelength, true);

                    //refractive index correction applied
                    vals[2] = vals[0] / vals[1];

                    //laser beam temperature
                    vals[9] = AverageLaserBeamTemperature;

                    //send of the averaging value for the DUT measurement
                    string result = "";

                    string num_s = asyc_meas.NSamples.ToString();
                    vals[8] = Convert.ToDouble(num_s);

                    if (asyc_meas.AveragingPending)
                    {


                        string sendstring = String.Concat("Averaging:", num_s);
                        if (!survey_device.Request(sendstring, ref result))
                        {
                            //invoke the gui to report a send/recevice error.
                            asyc_meas.mug(ProcNameMeasurement.NETWORK_TRANSMISSION, MeasurementError.EDM_SEND_RECEIVE_ERROR, true);
                            return false;
                        }
                        else
                        {
                            if (!result.Equals("true"))
                            {
                                //invoke the gui to report a data conversion error.
                                asyc_meas.mug(ProcNameMeasurement.DATA_CONVERSION, MeasurementError.EDM_FORMAT_ERROR, true);
                                return false;
                            }
                        }
                        asyc_meas.AveragingPending = false;
                    }

                    //If requested, reset the DUT
                    result = "";
                    //if (asyc_meas.DoReset)
                    //{
                    //    if (!survey_device.Request("Reset", ref result))
                    //    {
                    //invoke the gui to report a send/recevice error.
                    //        asyc_meas.mug(ProcNameMeasurement.NETWORK_TRANSMISSION, MeasurementError.EDM_SEND_RECEIVE_ERROR, true);
                    //       return false;
                    //   }
                    //    else
                    //   {
                    //       if (!result.Equals("true"))
                    //       {
                    //invoke the gui to report a data conversion error.
                    //           asyc_meas.mug(ProcNameMeasurement.DATA_CONVERSION, MeasurementError.EDM_FORMAT_ERROR, true);
                    //          return false;
                    //      }
                    // }
                    //}
                    //record a reading on the DUT.
                    //set the timeout 1st, depends on how many samples we are taking
                    survey_device.setTimeOut((int)asyc_meas.NSamples);

                    if (!survey_device.Request("Measure", ref result))
                    {
                        asyc_meas.mug(ProcNameMeasurement.NETWORK_TRANSMISSION, MeasurementError.EDM_SEND_RECEIVE_ERROR, true);
                        return false;
                    }
                    else
                    {
                        try
                        {
                            vals[3] = Convert.ToDouble(result);
                            asyc_meas.mug(Measurement.CurrentExecutionStage, result, true);
                        }
                        catch (FormatException)
                        {
                            asyc_meas.mug(ProcNameMeasurement.DATA_CONVERSION, MeasurementError.EDM_FORMAT_ERROR, true);
                            return false;
                        }
                    }



                    //get the stdev
                    if (!survey_device.Request("Stdev", ref result))
                    {
                        asyc_meas.mug(ProcNameMeasurement.NETWORK_TRANSMISSION, MeasurementError.EDM_SEND_RECEIVE_ERROR, true);
                        return false;
                    }
                    else
                    {
                        try
                        {
                            vals[7] = Convert.ToDouble(result);
                            asyc_meas.mug(ProcNameMeasurement.STDEV, result, true);
                        }
                        catch (FormatException)
                        {
                            asyc_meas.mug(ProcNameMeasurement.DATA_CONVERSION, MeasurementError.EDM_FORMAT_ERROR, true);
                            return false;
                        }
                    }

                    vals[10] = average_EDM_beam_temperature;


                    //remove the DUT default correction
                    if (device_type.Equals("Laser Aux"))
                    {
                        vals[4] = vals[3]; //aux laser refractive index must be set to 1;

                        //stop the gui from manipulating t,p,h while this executes;

                        vals[6] = calculateAverageRIEDMBeam(DUT.Beamfolds, ref asyc_meas.reflaser, ref asyc_meas.mug, DUTWavelength, true, true);
                    }
                    else if (device_type.Equals("EDM") || device_type.Equals("Total Station"))
                    {
                        vals[4] = vals[3] * (1 + EDM_TS_default_correction);
                        vals[6] = calculateAverageRIEDMBeam(DUT.Beamfolds, ref asyc_meas.reflaser, ref asyc_meas.mug, DUTWavelength, false, true);
                    }

                    vals[5] = vals[4] / vals[6];

                    vals[11] = Pressure;
                    vals[12] = asyc_meas.ref_barometer.Correction;
                    vals[14] = AverageHumidity;
                    vals[13] = asyc_meas.ref_logger1.Correction;
                    vals[15] = asyc_meas.ref_logger2.Correction;
                    vals[16] = CO2;


                    //note which prts we used for each beam
                    string laser_prts_ = "";
                    string EDM_prts_ = "";
                    int[] a = Measurement.LaserPRTSUsed;
                    if (a != null)
                    {

                        for (int i = 0; i < a.Length; i++)
                        {
                            laser_prts_ = string.Concat(laser_prts_, (a[i] + 1).ToString() + ":");
                        }

                        int[] b = Measurement.Row1PRTSUsed;
                        switch (DUT.Beamfolds)
                        {
                            case 0:


                                for (int i = 0; i < b.Length; i++)
                                {
                                    EDM_prts_ = string.Concat(EDM_prts_, (b[i] + 1).ToString() + ":");
                                }
                                break;
                            case 1:

                                for (int i = 0; i < b.Length; i++)
                                {
                                    EDM_prts_ = string.Concat(EDM_prts_, (b[i] + 1).ToString() + ":");
                                }
                                break;
                            case 2:
                                break;
                            case 3:
                                break;

                        }

                    }

                    string phase_prt_names = "";
                    string phase_temperatures = "";
                    string fold0_EDM_prt_names = "";
                    string fold0_temperatures = "";
                    string fold1_EDM_prt_names = "";
                    string fold1_temperatures = "";
                    string fold2_EDM_prt_names = "";
                    string fold2_temperatures = "";
                    string fold3_EDM_prt_names = "";
                    string fold3_temperatures = "";
                    string phase_pressure_names = "";
                    string phase_pressures = "";
                    string fold0_pressure_names = "";
                    string fold0_pressures = "";
                    string fold1_pressure_names = "";
                    string fold1_pressures = "";
                    string fold2_pressure_names = "";
                    string fold2_pressures = "";
                    string fold3_pressure_names = "";
                    string fold3_pressures = "";
                    string phase_humidity_names = "";
                    string phase_humidities = "";
                    string fold0_humidity_names = "";
                    string fold0_humidities = "";
                    string fold1_humidity_names = "";
                    string fold1_humidities = "";
                    string fold2_humidity_names = "";
                    string fold2_humidities = "";
                    string fold3_humidity_names = "";
                    string fold3_humidities = "";
                    for (int i = 0; i < 30; i++)
                    {
                        phase_prt_names = string.Concat(phase_prt_names, "Phase PRT ", (i + 1).ToString(), ",");
                        phase_temperatures = string.Concat(phase_temperatures, MeasurementPoint.phase_temperatures[i].ToString(), ",");
                        fold0_EDM_prt_names = string.Concat(fold0_EDM_prt_names, "Group Row 0 PRT ", (i + 1).ToString(), ",");
                        fold0_temperatures = string.Concat(fold0_temperatures, MeasurementPoint.group_temperatures1[i].ToString(), ",");
                        fold1_EDM_prt_names = string.Concat(fold1_EDM_prt_names, "Group Row 1 PRT ", (i + 1).ToString(), ",");
                        fold1_temperatures = string.Concat(fold1_temperatures, MeasurementPoint.group_temperatures2[i].ToString(), ",");
                        fold2_EDM_prt_names = string.Concat(fold2_EDM_prt_names, "Group Row 2 PRT ", (i + 1).ToString(), ",");
                        fold2_temperatures = string.Concat(fold2_temperatures, MeasurementPoint.group_temperatures3[i].ToString(), ",");
                        fold3_EDM_prt_names = string.Concat(fold3_EDM_prt_names, "Group Row 3 PRT ", (i + 1).ToString(), ",");
                        fold3_temperatures = string.Concat(fold3_temperatures, MeasurementPoint.group_temperatures4[i].ToString(), ",");

                        phase_pressure_names = string.Concat(phase_pressure_names, "Phase Pressure ", (i + 1).ToString(), ",");
                        phase_pressures = string.Concat(phase_pressures, MeasurementPoint.phase_pressures[i].ToString(), ",");
                        fold0_pressure_names = string.Concat(fold0_pressure_names, "Group Row 0 Pressure ", (i + 1).ToString(), ",");
                        fold0_pressures = string.Concat(fold0_pressures, MeasurementPoint.group_pressures1[i].ToString(), ",");
                        fold1_pressure_names = string.Concat(fold1_pressure_names, "Group Row 1 Pressure ", (i + 1).ToString(), ",");
                        fold1_pressures = string.Concat(fold1_pressures, MeasurementPoint.group_pressures2[i].ToString(), ",");
                        fold2_pressure_names = string.Concat(fold2_pressure_names, "Group Row 2 Pressure ", (i + 1).ToString(), ",");
                        fold2_pressures = string.Concat(fold2_pressures, MeasurementPoint.group_pressures3[i].ToString(), ",");
                        fold3_pressure_names = string.Concat(fold3_pressure_names, "Group Row 3 Pressure ", (i + 1).ToString(), ",");
                        fold3_pressures = string.Concat(fold3_pressures, MeasurementPoint.group_pressures4[i].ToString(), ",");

                        phase_humidity_names = string.Concat(phase_humidity_names, "Phase Humidity ", (i + 1).ToString(), ",");
                        phase_humidities = string.Concat(phase_humidities, MeasurementPoint.phase_humidities[i].ToString(), ",");
                        fold0_humidity_names = string.Concat(fold0_humidity_names, "Group Row 0 Humidity ", (i + 1).ToString(), ",");
                        fold0_humidities = string.Concat(fold0_humidities, MeasurementPoint.group_humidities1[i].ToString(), ",");
                        fold1_humidity_names = string.Concat(fold1_humidity_names, "Group Row 1 Humidity ", (i + 1).ToString(), ",");
                        fold1_humidities = string.Concat(fold1_humidities, MeasurementPoint.group_humidities2[i].ToString(), ",");
                        fold2_humidity_names = string.Concat(fold2_humidity_names, "Group Row 2 Humidity ", (i + 1).ToString(), ",");
                        fold2_humidities = string.Concat(fold2_humidities, MeasurementPoint.group_humidities3[i].ToString(), ",");
                        fold3_humidity_names = string.Concat(fold3_humidity_names, "Group Row 3 Humidity ", (i + 1).ToString(), ",");
                        fold3_humidities = string.Concat(fold3_humidities, MeasurementPoint.group_humidities4[i].ToString(), ",");

                    }
                    block = false;


                    //array to hold the values we measure 0 = laser raw reading, 1 = RI correction laser, 2 = Laser with MSL RI Correction applied ,
                    //                                    3 = DUT raw reading, 4 = DUT with default correction removed, 5 = DUT with MSL RI Correction applied ,  
                    //                                    6 = DUT RI Correction, 7 = DUT standard deviation, 8 = DUT averaging  
                    //                                    9 = Laser Beam Temperature, 10 = DUT beam Temperature,11= Pressure,12 = Average Humidity,13 = DateTime 

                    //7.store the values returned
                    switch (Measurement.CurrentExecutionStage)
                    {

                        case ExecutionStage.START:
                            asyc_meas.start_pos_value = vals;

                            string version = "Software Version 1.1";
                            string config_file = "Configuration File Name: " + asyc_meas.ConfigFileName;

                            string line_title = "Position,Laser Raw,RI Correction Laser,Laser with Phase RI Correction,DUT Raw Reading,DUT with Default Correction Removed,DUT with group RI applied,DUT Group RI Correction,DUT Standard Deviation,DUT averaging,Laser Beam Temperature,Average DUT beam Temperature,Average Pressure,Average Humidity,Barometer Correction, Humidity Logger 1 Correction, Humidity Logger 2 Correction, CO2 Concentration, DateTime,Laser PRTS Used, EDM PRTS Used," + phase_prt_names + fold0_EDM_prt_names + fold1_EDM_prt_names + fold2_EDM_prt_names + fold3_EDM_prt_names + phase_pressure_names + fold0_pressure_names + fold1_pressure_names + fold2_pressure_names + fold3_pressure_names + phase_humidity_names + fold0_humidity_names + fold1_humidity_names + fold2_humidity_names + fold3_humidity_names;

                            string line = "Start," + vals[0].ToString() + "," + vals[1].ToString() + "," + vals[2].ToString()
                                        + "," + vals[3].ToString() + "," + vals[4].ToString() + "," + vals[5].ToString()
                                        + "," + vals[6].ToString() + "," + vals[7].ToString() + "," + vals[8].ToString()
                                        + "," + vals[9].ToString() + "," + vals[10].ToString() + "," + vals[11].ToString()
                                        + "," + vals[12].ToString() + "," + vals[13].ToString() + "," + vals[14].ToString()
                                        + "," + vals[15].ToString() + "," + vals[16].ToString()
                                        + "," + DateTime.Now.ToString() + "," + laser_prts_ + "," + EDM_prts_ + ","
                                        + phase_temperatures + fold0_temperatures + fold1_temperatures + fold2_temperatures + fold3_temperatures
                                        + phase_pressures + fold0_pressures + fold1_pressures + fold2_pressures + fold3_pressures
                                        + phase_humidities + fold0_humidities + fold1_humidities + fold2_humidities + fold3_humidities;



                            if (!asyc_meas.WriteFile(version))
                            {
                                asyc_meas.mug(ProcNameMeasurement.FILE_WRITE, MeasurementError.FILE_ERROR, true);
                            }
                            if (!asyc_meas.WriteFile(config_file))
                            {
                                asyc_meas.mug(ProcNameMeasurement.FILE_WRITE, MeasurementError.FILE_ERROR, true);
                            }
                            if (!asyc_meas.WriteFile(""))
                            {
                                asyc_meas.mug(ProcNameMeasurement.FILE_WRITE, MeasurementError.FILE_ERROR, true);
                            }
                            if (!asyc_meas.WriteFile(line_title))
                            {
                                asyc_meas.mug(ProcNameMeasurement.FILE_WRITE, MeasurementError.FILE_ERROR, true);
                            }

                            if (!asyc_meas.WriteFile(line))
                            {
                                asyc_meas.mug(ProcNameMeasurement.FILE_WRITE, MeasurementError.FILE_ERROR, true);
                            }

                            Measurement.CurrentExecutionStage = ExecutionStage.INTERDIATE;
                            break;
                        case ExecutionStage.INTERDIATE:
                            asyc_meas.setIntermediateValue(vals, array_index);

                            string line1 = "Intermediate," + vals[0].ToString() + "," + vals[1].ToString() + "," + vals[2].ToString()
                                        + "," + vals[3].ToString() + "," + vals[4].ToString() + "," + vals[5].ToString()
                                        + "," + vals[6].ToString() + "," + vals[7].ToString() + "," + vals[8].ToString()
                                        + "," + vals[9].ToString() + "," + vals[10].ToString() + "," + vals[11].ToString()
                                        + "," + vals[12].ToString() + "," + vals[13].ToString() + "," + vals[14].ToString()
                                        + "," + vals[15].ToString() + "," + vals[16].ToString()
                                        + "," + DateTime.Now.ToString() + "," + laser_prts_ + "," + EDM_prts_ + ","
                                        + phase_temperatures + fold0_temperatures + fold1_temperatures + fold2_temperatures + fold3_temperatures
                                        + phase_pressures + fold0_pressures + fold1_pressures + fold2_pressures + fold3_pressures
                                        + phase_humidities + fold0_humidities + fold1_humidities + fold2_humidities + fold3_humidities;

                            if (!asyc_meas.WriteFile(line1))
                            {
                                asyc_meas.mug(ProcNameMeasurement.FILE_WRITE, MeasurementError.FILE_ERROR, true);
                            }

                            if (array_index == asyc_meas.number_int_values)
                            {
                                Measurement.CurrentExecutionStage = ExecutionStage.END;
                            }

                            break;
                        case ExecutionStage.END:
                            asyc_meas.end_pos_value = vals;

                            string line2 = "END," + vals[0].ToString() + "," + vals[1].ToString() + "," + vals[2].ToString()
                                        + "," + vals[3].ToString() + "," + vals[4].ToString() + "," + vals[5].ToString()
                                        + "," + vals[6].ToString() + "," + vals[7].ToString() + "," + vals[8].ToString()
                                        + "," + vals[9].ToString() + "," + vals[10].ToString() + "," + vals[11].ToString()
                                        + "," + vals[12].ToString() + "," + vals[13].ToString() + "," + vals[14].ToString()
                                        + "," + vals[15].ToString() + "," + vals[16].ToString()
                                        + "," + DateTime.Now.ToString() + "," + laser_prts_ + "," + EDM_prts_ + ","
                                        + phase_temperatures + fold0_temperatures + fold1_temperatures + fold2_temperatures + fold3_temperatures
                                        + phase_pressures + fold0_pressures + fold1_pressures + fold2_pressures + fold3_pressures
                                        + phase_humidities + fold0_humidities + fold1_humidities + fold2_humidities + fold3_humidities;

                            if (!asyc_meas.WriteFile(line2))
                            {
                                asyc_meas.mug(ProcNameMeasurement.FILE_WRITE, MeasurementError.FILE_ERROR, true);
                            }

                            //We have reached the end of this measurement.  Invoke the gui to see if there is more to do.
                            asyc_meas.mug(ProcNameMeasurement.EXECUTION_COMPLETE, MeasurementError.NO_ERROR, false);
                            Measurement.current_execution_stage = ExecutionStage.IDLE;
                            break;
                    }
                }
            }
            else
            {
                //can't connect to the DUT.
                asyc_meas.mug(ProcNameMeasurement.ISCONNECTED, MeasurementError.CONNECTION_ERROR, true);
                return false;
            }

            return true;

        }

        public bool WriteFile(string line)
        {
            //invoke the gui to write the file
            mug(ProcNameMeasurement.FILE_WRITE, line, false);
            return true;
        }

    }
}