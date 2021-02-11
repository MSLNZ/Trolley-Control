using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ivi.Visa.Interop;
using System.Windows.Forms;
using System.Threading;


namespace Trolley_Control
{
    /// <summary>
    /// A class to serve temperature data
    /// </summary>
    public abstract class GPIBOverLANCommands
    {
        private static FormattedIO488 ioDmm;
        //private ITcpipInstr iodss;
        private static string error_status;
        protected static int GPIB_adr = 0;
        protected static string SICL_interface_id = "";
        protected string ss;
        protected string init_string;

        public GPIBOverLANCommands()
        {

        }
        public int GPIBAddr
        {
            get { return GPIB_adr; }
        }
        public string GPIBSICL
        {
            get { return SICL_interface_id; }
        }
        public static void createInteropObject()
        {
            try
            {
                //create the formatted io object
                ioDmm = new FormattedIO488();
                //iodss = new ITcpipInstr();
            }
            catch (SystemException ex)
            {
                error_status = "FormattedIO488Class object creation failure. " + ex.Source + "  " + ex.Message;
                goto END;
            }


            error_status = "No Error";
        END:;
        }


        public void InitIO(string sendstring)
        {
            ss = sendstring;
            init_string = ss;
            try
            {

                //create the resource manager and open a session with the instrument specified on txtAddress
                ResourceManager grm = new ResourceManager();


                ioDmm.IO = (IMessage)grm.Open(sendstring, AccessMode.NO_LOCK, 2000, "");


            }
            catch (SystemException ex)
            {
                ioDmm.IO = null;
                error_status = "Open failed on " + sendstring + " " + ex.Source + "  " + ex.Message;

            }
            error_status = "No Error";

        }

        protected void CloseConnection()
        {
            //close the session
            ioDmm.IO.Close();
        }

        protected string ReadInstrumentIDN()
        {
            // Retrieve the string from instrument
            string idn_value;

            try
            {
                //Gets the instrument model number
                ioDmm.WriteString("IDN?", true);
                idn_value = ioDmm.ReadString();
            }
            catch (SystemException ex)
            {
                error_status = "Failed to retrieve default settings. Error: " + ex.Message;
                return error_status;
            }
            error_status = "No Error";
            return idn_value;
        }

        protected void sendcommand(string command)
        {
            //Gets the instrument model number
            while (true)
            {
                //there can be a fairly serious bug here if this thread hangs during the execution of the following command
                try
                {

                    ioDmm.WriteString(command, true);

                    break;
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                    //try flushing the buffers and resending waiting a bit and sending again
                    //If it falls over here it is probably due to a problem a network issue.
                    try
                    {
                        if (ioDmm.IO != null)
                        {
                            ioDmm.IO.Close();
                            Thread.CurrentThread.Join(5000);
                        }
                        ioDmm = new FormattedIO488();
                        //create the resource manager and open a session with the instrument specified on txtAddress     
                        ResourceManager grm = new ResourceManager();
                        ioDmm.IO = (IMessage)grm.Open("GPIB3::3", AccessMode.NO_LOCK, 2000, "");         //this is set to null if io is down and it triggers a com exception
                    }

                    catch (System.Runtime.InteropServices.COMException)
                    {
                        Thread.CurrentThread.Join(5000);
                        continue;
                    }
                }
                catch (Exception e)
                {
                    return;
                }
            }
        }

        protected string ReadInstrument(string sendstring)
        {
            string idn_value;

            try
            {
                //Gets the instrument model number
                ioDmm.WriteString(sendstring, true);
                idn_value = ioDmm.ReadString();
            }
            catch (SystemException ex)
            {
                error_status = "Failed to retrieve default settings. Error: " + ex.Message;
                return error_status;
            }
            error_status = "No Error";
            return idn_value;
        }

        protected string ReadInstrument(string sendstring, ref RichTextBox progress_window)
        {
            string idn_value;
            try
            {

                //Gets the instrument model number
                ioDmm.WriteString(sendstring, true);
                idn_value = ioDmm.ReadString();
                progress_window.AppendText(idn_value + "\n");
                error_status = "No Error\n";
                return idn_value;
            }
            catch (SystemException ex)
            {
                progress_window.AppendText("Failed to retrieve default settings. Error: " + ex.Message + "\n");
                error_status = "ERROR\n";
                return error_status;
            }
        }

        protected void ReadInstrument(string sendstring, ref string value)
        {
            string idn_value;

            try
            {
                //Gets the instrument model number
                ioDmm.WriteString(sendstring, true);
                idn_value = ioDmm.ReadString();
                error_status = "No Error\n";
                value = idn_value;
            }
            catch (SystemException)
            {
                error_status = "ERROR\n";
                value = error_status;
            }
        }
        protected void ReadResponse(ref string val)
        {
            try
            {

                val = ioDmm.ReadString();

            }
            catch (System.Runtime.InteropServices.COMException)
            {

            }
        }
        public void setDeviceAdr(short address)
        {
            GPIB_adr = address;
        }
    }
}