using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Trolley_Control
{
    public class AgilentMUX:MUX
    {
        private string append_string;
      
        private Object thislock = new Object();

        //Since the agilient MUX and bridge are actually one unit the constructor here only needs to take
        //one argument.  The address and the SICL are not required because they are contain in the agilent bridge.
        public AgilentMUX(ref PRT[] prts_connected):base(ref prts_connected)
        {
            base.prts = prts_connected;      //The PRTs that are currently connected to the mux
            append_string = "(@101)\r\n";    //default append string
            base.selected_channel = 1;
        }

        /// <summary>
        /// Sets what channel the multiplexor is switched to
        /// creates a string to append to the Bridge send command
        /// </summary>
        /// <param name="channel_number">channel number is a value between 1 and 30</param>
        public override void setChannel(short channel_number)
        {
            lock (thislock)
            {
                if ((channel_number > 0) && (channel_number <= 10))
                {
                    if (channel_number < 10)
                    {
                        append_string = string.Concat("(@10", channel_number.ToString(), ")\r\n");
                    }
                    else append_string = "(@110)\r\n"; 
                }
                else if ((channel_number > 10) && (channel_number <= 20))
                {
                    if (channel_number < 20)
                    {
                        append_string = string.Concat("(@20", (channel_number-10).ToString(), ")\r\n");
                    }
                    else append_string = "(@210)\r\n";
                }
                else if ((channel_number > 20) && (channel_number <= 30))
                {
                    if (channel_number < 30)
                    {
                        
                        append_string = string.Concat("(@30", (channel_number-20).ToString(), ")\r\n");
                        
                    }
                    else append_string = "(@310)\r\n";
                    
                }
                selected_channel = channel_number;
            }
        }

        /// <summary>
        /// -Gets the channel the mux is set to
        /// </summary>
        /// <param name="probe_name">A channel type</param>
        public override short getCurrentChannel()
        {
            return selected_channel;

        }

        /// <summary>
        /// -Gets the probe that is plugged into a given channel
        /// -30 prts can be connected to this type of multiplexor
        /// </summary>
        /// <param name="probe_name">The channel from 1 to 30</param>
        public override PRT getProbe(string channel_)
        {
            PRT probe_on_this_channel = null;
            switch (channel_)
            {
                case "CH1":
                    probe_on_this_channel = prts[0];
                    break;
                case "CH2":
                    probe_on_this_channel = prts[1];
                    break;
                case "CH3":
                    probe_on_this_channel = prts[2];
                    break;
                case "CH4":
                    probe_on_this_channel = prts[3];
                    break;
                case "CH5":
                    probe_on_this_channel = prts[4];
                    break;
                case "CH6":
                    probe_on_this_channel = prts[5];
                    break;
                case "CH7":
                    probe_on_this_channel = prts[6];
                    break;
                case "CH8":
                    probe_on_this_channel = prts[7];
                    break;
                case "CH9":
                    probe_on_this_channel = prts[8];
                    break;
                case "CH10":
                    probe_on_this_channel = prts[9];
                    break;
                case "CH11":
                    probe_on_this_channel = prts[10];
                    break;
                case "CH12":
                    probe_on_this_channel = prts[11];
                    break;
                case "CH13":
                    probe_on_this_channel = prts[12];
                    break;
                case "CH14":
                    probe_on_this_channel = prts[13];
                    break;
                case "CH15":
                    probe_on_this_channel = prts[14];
                    break;
                case "CH16":
                    probe_on_this_channel = prts[15];
                    break;
                case "CH17":
                    probe_on_this_channel = prts[16];
                    break;
                case "CH18":
                    probe_on_this_channel = prts[17];
                    break;
                case "CH19":
                    probe_on_this_channel = prts[18];
                    break;
                case "CH20":
                    probe_on_this_channel = prts[19];
                    break;
                case "CH21":
                    probe_on_this_channel = prts[20];
                    break;
                case "CH22":
                    probe_on_this_channel = prts[21];
                    break;
                case "CH23":
                    probe_on_this_channel = prts[22];
                    break;
                case "CH24":
                    probe_on_this_channel = prts[23];
                    break;
                case "CH25":
                    probe_on_this_channel = prts[24];
                    break;
                case "CH26":
                    probe_on_this_channel = prts[25];
                    break;
                case "CH27":
                    probe_on_this_channel = prts[26];
                    break;
                case "CH28":
                    probe_on_this_channel = prts[27];
                    break;
                case "CH29":
                    probe_on_this_channel = prts[28];
                    break;
                case "CH30":
                    probe_on_this_channel = prts[30];
                    break;
                  
            }
            return probe_on_this_channel;
        }
        public override void setProbe(PRT new_PRT,short channel_is_on)
        {
            try
            {
                prts[channel_is_on - 1] = new_PRT;
              
            }
            catch (IndexOutOfRangeException)
            {
                channel_is_on = 1;
                prts[channel_is_on - 1] = new_PRT;
                
            }
        }

        /// <summary>
        /// -Gets the channel append string to be send with the agilent bridge command, of the form "(@XXX)" 
        /// </summary>
        public string AppendString
        {
            get {return append_string;}
        }


    }
}
