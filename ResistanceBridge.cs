using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Trolley_Control
{
    public abstract class ResistanceBridge
    {


        protected Object thislock = new Object();
        protected double correctionA1_1;
        protected double correctionA2_1;
        protected double correctionA3_1;
        protected double correctionA4_1;
        protected double correctionA1_2;
        protected double correctionA2_2;
        protected double correctionA3_2;
        protected double correctionA4_2;
        protected double correctionA1_3;
        protected double correctionA2_3;
        protected double correctionA3_3;
        protected double correctionA4_3;
        protected double correctionA1_4;
        protected double correctionA2_4;
        protected double correctionA3_4;
        protected double correctionA4_4;


        protected short current_channel_in_use;

        public ResistanceBridge()
        {
      
        }

        protected abstract void setRemoteMode();


        /// <summary>
        /// - Current must be between 0 and 3 which equates to 0.1mA, 0.3mA, 1mA and 3mA.
        /// </summary>
        /// <param name="current">A value betweem 0 and 3</param>
        protected abstract void setCurrent(short current);


        protected abstract void Init();

        /// <summary>
        /// -Returns the current temperature in degrees C
        /// </summary>
        /// <param name="multiplexor_channel">channel number is a value between 1 and 30</param>
        public abstract double getTemperature(PRT probe_type, short channel_number, bool probe_has_changed);

        /// <summary>
        /// -Gets a probe with the specified channel type
        /// </summary>
        /// <param name="multiplexor_channel">A channel type</param>
        protected PRT getProbe(string probe_name)
        {
            return multi.getProbe(probe_name);
        }
        protected short getCurrentChannel()
        {
            return current_channel_in_use;
        }

        public double A1_1
        {
            get { return correctionA1; }
            set { correctionA1 = value; }
        }
        public double A2_1
        {
            get { return correctionA2; }
            set { correctionA2 = value; }
        }
        public double A3_1
        {
            get { return correctionA3; }
            set { correctionA3 = value; }
        }

        public double A4_1
        {
            get { return correctionA4; }
            set { correctionA4 = value; }
        }

        public double A1_2
        {
            get { return correctionA1_2; }
            set { correctionA1_2 = value; }
        }
        public double A2_2
        {
            get { return correctionA2_2; }
            set { correctionA2_2 = value; }
        }
        public double A3_2
        {
            get { return correctionA3_2; }
            set { correctionA3_2 = value; }
        }

        public double A4_2
        {
            get { return correctionA4_2; }
            set { correctionA4_2 = value; }
        }
        public double A1_3
        {
            get { return correctionA1_3; }
            set { correctionA1_3 = value; }
        }
        public double A2_3
        {
            get { return correctionA2_3; }
            set { correctionA2_3 = value; }
        }
        public double A3_3
        {
            get { return correctionA3_3; }
            set { correctionA3_3 = value; }
        }

        public double A4_3
        {
            get { return correctionA4_3; }
            set { correctionA4_3 = value; }
        }

        public double A1_4
        {
            get { return correctionA1_4; }
            set { correctionA1_4 = value; }
        }
        public double A2_4
        {
            get { return correctionA2_4; }
            set { correctionA2_4     = value; }
        }
        public double A3_4
        {
            get { return correctionA3_4; }
            set { correctionA3_4 = value; }
        }

        public double A4_4
        {
            get { return correctionA4_4; }
            set { correctionA4_4 = value; }
        }

        public void setCurrentChannel(short channel)
        {
            lock (thislock)
            {
                multi.setChannel(channel);
            }
        }

        /// <summary>
        /// -Removes the bad stuff out of the string so that it can be converted to a double
        /// </summary>
        /// <param name="multiplexor_channel">A channel type</param>
        protected string ParseResistanceString(string resistance)
        {
            if (resistance.Contains('+'))
            {
                int index = resistance.IndexOf('+');
                resistance.Remove(index, 1);
            }
            return resistance;
        }

    }
}