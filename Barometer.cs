using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trolley_Control
{
    public struct BarometerExecutionStage
    {
        public const short SETUP = 0;
        public const short POLL = 1;
        public const short TERMINATE = 2;
        public const short INIT = 3;
        public const short IDLE = 255;
    }

    public abstract class Barometer
    {
        protected double current_correction = 0.0;
        protected short current_exe_stage;
        protected bool error_reported = false;
        protected string communication_string = "COM1";  //could be a COM Port or an IP Address depending on Barometer type.
        protected string report_number;
        protected string report_date;
        protected string equipment_reg_id;
        private double[] rising_pressures = new double[11];
        private double[] falling_pressures = new double[11];
        private double[] pressure_thresholds = { 950, 960, 970, 980, 990, 1000, 1010, 1020, 1030, 1040, 1050 };
        protected string correction_equation;
        protected bool slope; //rising or falling
        protected bool rising_falling_valid;
        protected double result = 0.00;


        public string CommunicationString
        {
            set { communication_string = value; }
            get { return communication_string; }
        }



        public string ReportNum
        {
            set { report_number = value; }
            get { return report_number; }
        }
        public string ReportDate
        {
            set { report_date = value; }
            get { return report_date; }
        }
        public string EquipRegisterID
        {
            set { equipment_reg_id = value; }
            get { return equipment_reg_id; }
        }

        public double Correction
        {
            set { current_correction = value; }
            get { return current_correction; }
        }
        public abstract void Close();

        public abstract bool IsOpen();

        public abstract double getPressure();

        public bool ErrorReported
        {
            set { error_reported = value; }
            get { return error_reported; }
        }
        public void setExecStage(short e)
        {
            current_exe_stage = e;
        }

        public void ParseCorrectionStrings(string[] correctionstrings)
        {

            for (int i = 0; i < correctionstrings.Length; i++)
            {
                int index_of_colon = correctionstrings[i].IndexOf(':');

                rising_pressures[i] = Convert.ToDouble(correctionstrings[i].Remove(index_of_colon));
                falling_pressures[i] = Convert.ToDouble(correctionstrings[i].Substring(index_of_colon + 1));

            }
        }
        protected void CalculateCorrection(double result)
        {
            int index_of_correction = 0;
            for (int i = 0; i < 11; i++)
            {
                if (Math.Abs(result - pressure_thresholds[i]) <= 5)
                {
                    index_of_correction = i;
                }
            }

            if (slope && rising_falling_valid)
            {
                current_correction = rising_pressures[index_of_correction];
            }
            else if (!slope && rising_falling_valid)
            {
                current_correction = falling_pressures[index_of_correction];
            }
            else
            {
                current_correction = ((rising_pressures[index_of_correction] + falling_pressures[index_of_correction]) / 2);
            }

        }
    }
}