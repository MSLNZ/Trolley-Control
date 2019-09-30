using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trolley_Control
{

    /// <summary>
    /// The device under test class represents all the possible types of of device under test, e.g and EDM, A total station, A second laser.
    /// </summary>
    public abstract class DUT
    {

        protected static Client TCPClient;
      
        protected static bool connection_pending;
        protected static string host_name;
        protected static DUTUpdateGui dutug;
        protected int unit_timeout;
        protected static short beam_folds;
        protected static string device_name="EDM";
        protected static readonly int port = 16;
        

        public DUT(DUTUpdateGui dutug_)
        {
            TCPClient = new Client();
            dutug = dutug_;
            connection_pending = true;
            //host_name = "";
        }

        
        public abstract bool Request(String request, ref string result);

        public abstract void setTimeOut(int num_samples);

        public static string deviceType{

            get { return device_name; }


        }

        public static short Beamfolds
        {

            get { return beam_folds; }
            set { beam_folds = value; }
        }

        public static bool Disconnect()
        {
            try
            {
                TCPClient.closeConnection();
                return true;
            }
            catch (System.IO.IOException)
            {
                return false;
            }
        }

        public static bool Connected()
        {
            return TCPClient.isConnected();
        }

        
       

        public static bool TCPConnectionPending
        {
            set
            {
                connection_pending = value;
            }
            get
            {
                return connection_pending;
            }
        }
        public static string HostName
        {
            get
            {
                return host_name;
            }
            set
            {
                host_name = value;
            }
        }



        public static bool TryConnect()
        {
  
            bool return_value = true;

            //if we need to connect then try
            if (connection_pending)
            {
                if (!TCPClient.Connect(HostName,port))
                {
                    //invoke the gui to say we can't get a Network connection
                    dutug(ProcNameMeasurement.ISCONNECTED, MeasurementError.CONNECTION_ERROR, true);
                    return_value = false;
                }
                connection_pending = false;
            }
            return return_value;
        }

      

    }
}
