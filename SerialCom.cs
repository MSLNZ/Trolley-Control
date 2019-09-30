using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Threading;

namespace Trolley_Control
{
    public abstract class SerialCom
    {
        public static SerialPort s_port;
        
        protected VaisalaUpdateGui update_gui;
        protected bool error_reported = false;
        protected short current_exe_stage;
        protected double result = 0.00;
        protected bool is_open = false;
        protected string last_error = "ERROR code not specified";
        private double correction = 0.0;

        public SerialCom()
        {
            
        }

        public double getPressure
        {
            get { return result+correction; }
        }
        public double Correction
        {
            set { correction = value; }
            get { return correction; }
        }

        public bool ErrorReported
        {
            set { error_reported = value; }
            get { return error_reported; }
        }

        public bool IsOpen
        {
            get { return is_open; }
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
        public void Close()
        {
            s_port.Close();
        }

        public abstract bool CheckComPorts();

        public abstract bool Init(string portname);

        public abstract bool Read();

      
    }
}
