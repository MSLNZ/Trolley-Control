using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;


namespace Trolley_Control
{

    public struct ProcNameHumidity
    {
        public const short CONNECT = 0;
        public const short SEND_RECEIVE = 1;
        public const short EQUATION_FORMAT = 2;
        public const short IDLE = 255;
    }

   
    public class OmegaTHLogger
    {
        THLoggerUpdateGUI h_update;
        
        private Client TcpClient;
        private string hostname;
        private string Hloggereq = "";
        private static short num_connected_loggers = 0;
        private int timer_zero1;
        private int timer_zero2;
        private int timer_1;
        private int timer_2;
        private double humidity_result = 50.00;
        private bool error_reported=false;
        private bool isactive = false;
        private short dev_id = 255;
        private double correction = 0.0;
        protected static readonly int port = 14;
        private string report_num = "";
        private string report_date = "";
        private string equip_id = "";
        private string type = "";
        private string location = "";
        private string rawcorrection = "";





        public OmegaTHLogger(string hostname_, ref THLoggerUpdateGUI th_update)
        {
            TcpClient = new Client();
            h_update = th_update;
            hostname = hostname_;
        }

        public string ReportNumber
        {
            set { report_num = value; }
            get { return report_num; }
        }

        public string ReportDate
        {
            set { report_date = value; }
            get { return report_date; }
        }

        public string EquipID
        {
            set { equip_id = value; }
            get { return equip_id; }
        }

        public string Type
        {
            set { type = value; }
            get { return type; }
        }

        public string IP
        {
            set { hostname = value; }
            get { return hostname; }
        }

        public string Location
        {
            set { location = value; }
            get { return location; }
        }

        public string RawCorrection
        {
            set { rawcorrection = value; }
            get { return rawcorrection; }
        }

        public string HLoggerEq
        {
            set { Hloggereq = value; }
            get { return Hloggereq; }
        }

        public double getHu()
        {
            //CalculateCorrection();

            return humidity_result+correction; 
        }

        public double Correction
        {
            get { return correction; }
            set { correction = value; }
        }
        public short devID
        {
            get { return dev_id; }
            set { dev_id = value; }
        }

        public void CalculateCorrection()
        {
            bool error;
            bool remove = true;
            int pos_of_start = 0;
            int pos_of_R=0;
            int pos_of_R2=0;
            int pos_of_R3=0;
            int a_sign = 0;
            int b_sign = 0;
            int c_sign = 0;
            int d_sign = 0;
            string a = "";
            string b = "";
            string c = "";
            string d = "";
            string remainder;

            char a_signbit = HLoggerEq[0];

            if ((a_signbit == '-') || (a_signbit == '+'))
            {

                remove = true;
            }
            else {
                a_signbit = '+';
                remove = false;
            }

            if (remove == true)
            {
                remainder = HLoggerEq.Substring(1);
            }
            else remainder = HLoggerEq;

            pos_of_R = remainder.IndexOf('R');
            if (remainder.IndexOf('+') < pos_of_R)
            {
               
                a = remainder.Remove(remainder.IndexOf('+'));
                remainder = remainder.Substring(remainder.IndexOf('+'));
            }
            else if (remainder.IndexOf('-') < pos_of_R)
            {
                
                a = remainder.Remove(remainder.IndexOf('-'));
                remainder = remainder.Substring(remainder.IndexOf('-'));
            }

            try {
                b = remainder.Remove(remainder.IndexOf('R'));
                remainder = remainder.Substring(remainder.IndexOf('R') + 1);

                c = remainder.Remove(remainder.IndexOf('R'));
                remainder = remainder.Substring(remainder.IndexOf('R') + 2);

                d = remainder.Remove(remainder.IndexOf('R'));
               
                }
            catch (ArgumentOutOfRangeException)
            {
                h_update(ProcNameHumidity.EQUATION_FORMAT, "The equation formatting for the humidity device is not recognised", true);
            }

            a = a_signbit + a;

            try
            {
                double a_ = Convert.ToDouble(a);
                double b_ = Convert.ToDouble(b);
                double c_ = Convert.ToDouble(c);
                double d_ = Convert.ToDouble(d);
                double currentH = getHu();
                
                    correction = a_ + b_*currentH+c_*Math.Pow(currentH,2)+d_*Math.Pow(currentH,3);
               
            }
            catch (FormatException)
            {
                return;
            }
        }

        public void HLoggerQuery(object stateinfo)
        {

            timer_zero1 = Environment.TickCount;
            timer_zero2 = Environment.TickCount;
            timer_1 = timer_zero1 + 10000;
            timer_2 = timer_zero2 + 20000;

            while (true)
            {

                //get the latest times
                timer_1 = Environment.TickCount;
                timer_2 = Environment.TickCount;
                
                
                //if we haven't had a valid humidity reading for more than 30 s then set to inactive
                if(timer_2 > timer_zero2 + 30000)
                {
                    if (isactive == true )num_connected_loggers--;
                    isactive = false;
                    
                }

                //check if we are connected
                if (TcpClient.isConnected())
                {

                   
                    
                    string result="";
                    if (TcpClient.sendReceiveData("*SRH\r", ref result))
                    {
                        try
                        {
                            double result_ = Convert.ToDouble(result);
                            humidity_result = result_;
                            error_reported = false;
                            h_update(ProcNameHumidity.SEND_RECEIVE, "No Error", false);
                            if(isactive == false) num_connected_loggers++;
                            isactive = true;
                            
                            timer_zero2 = Environment.TickCount;
                            error_reported = false;
                        }
                        catch (FormatException e)
                        {
                            h_update(ProcNameHumidity.SEND_RECEIVE, e.ToString(), true);
                            continue;
                        }
                    }
                    else
                    {
                        if (!error_reported)
                        {
                            h_update(ProcNameHumidity.SEND_RECEIVE, "An orror occured sending/receiving data from the humidity device. This means one or more data requests to the TH logger failed.  It seems as though this is normal behaviour for the Omega loggers. If humidity values are being reported in the GUI, then you can assume that things are working as they should be.", true);   //error not reported - report
                            error_reported = true;
                        }
                    }

                    
                }
                else
                {
                    //we're not connected - attempt to connect. We don't want to do this too often because it has a high overhead, try connecting every 10s
                    if (timer_1 >= timer_zero1 + 10000) {
                        if (!TryConnect())
                        {
                            if (!error_reported)
                            {
                                h_update(ProcNameHumidity.CONNECT, "An error occurred connecting to Humidity device " + devID.ToString(), true);
                                error_reported = true;
                            }
                        }
                        timer_zero1 = Environment.TickCount;
                    }
                }
                Thread.Sleep(20000);  //we only sample the logger every 20 seconds
            }

        }

        public bool TryConnect()
        {
            return TcpClient.Connect(hostname,port);
        }

        public void setHostName(string hostname_)
        {
            hostname = hostname_;
        }
        public bool isActive
        {
            get { return isactive;  }
        }

        public static short numConnectedLoggers
        {
            get { return num_connected_loggers; }
        }  
    }
}
