using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Trolley_Control
{
    public abstract class MUX:GPIBOverLANCommands
     {
        protected const int multiplexor_adr = 14;
        protected volatile short selected_channel;
        protected PRT[] prts;
        
        


        public MUX(int GPIB_Address_, string SICL_, ref PRT[] prts_connected)
        {
            GPIB_adr = GPIB_Address_;
            SICL_interface_id = SICL_;
            prts = prts_connected;
            selected_channel = 1;
        }

        public MUX(ref PRT[] prts_connected)
        {
            prts = prts_connected;
            selected_channel = 1;
        }

        /// <summary>
        /// Sets what channel the multiplexor is switched to
        /// </summary>
        /// <param name="channel_number">channel number is a value between 1 and 9</param>
        public abstract void setChannel(short channel_number);

        /// <summary>
        /// -Gets the channel the mux is set to
        /// </summary>
        /// <param name="probe_name">A channel type</param>
        public abstract short getCurrentChannel();

        /// <summary>
        /// -Gets a probe with the specified channel type
        /// </summary>
        /// <param name="channel_">A channel type</param>
        public abstract PRT getProbe(string channel_);

        public abstract void setProbe(PRT new_PRT, short channel_is_on);
    }
}
