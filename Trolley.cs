using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Threading;

namespace Trolley_Control
{ 
    public struct TrolleyError
    {
        public const string msg1 = "Check the COM Port is correct and the bluetooth connection is established via the LM technologies software"
                     + "The lm technologies software needs to running on this computer";
        public const string msg2 = "Bluetooth communication failure";
        
    }
    public struct ProcNameTrolley
    {
        public const short OPEN = 0;
        public const short GO = 1;
        public const short STOP = 2;
        public const short FORWARD = 3;
        public const short REVERSE = 4;
        public const short SETSPEED = 5;
        public const short BLUETOOTH_CONNECTION = 100;
        public const short BLUETOOTH_AVAILIBLE = 101;
        public const short IDLE = 255;
 
    }

    
    public class Trolley
    {

        private TrolleyUpdateGUI invoke_gui;
        private SerialPort port;
        private bool errorstate;
        private byte[] speed_byte;
        private _32Feet_BlueTooth b_tooth;
        private static Object lockthis;
        short proc_to_do = ProcNameTrolley.IDLE;
      

        public Trolley(ref TrolleyUpdateGUI gui_updater, ref SerialPort s_port)
        {
          
            port = s_port;
            invoke_gui = gui_updater;
            errorstate = false;
            lockthis = new Object();
            b_tooth = new _32Feet_BlueTooth(invoke_gui);
           
        }

        public bool Errorstate
        {
            set { errorstate = value; }
            get { return errorstate; }
        }

        public bool isPortOpen
        {
            get
            {
                return b_tooth.Connected;
            }
        }

        public byte[] SpeedByte
        {
            set
            {
                speed_byte = value;
            }
            get
            {
                return speed_byte;
            }
        }

        public short ProcToDo
        {
            get
            {
                return proc_to_do;
            }
            set
            {
                proc_to_do = value;
            }
        }

        public bool Forward()
        {
         
            byte[] command1 = new byte[4];


            command1[0] = (byte)'R';
            command1[1] = (byte)'B';
            command1[2] = (byte)'C';
            command1[3] = (byte)'e';

            if (errorstate == false)
            { 
            if (b_tooth.sendData(command1, 0, 4)) return true;
            else return false;
            }
            return false;
           

        }



        public bool Reverse()
        {
         
            byte[] command1 = new byte[4];

            command1[0] = (byte)'R';
            command1[1] = (byte)'b';
            command1[2] = (byte)'c';
            command1[3] = (byte)'e';
            if (errorstate == false){
                if (b_tooth.sendData(command1, 0, 4)) return true;
                else return false;
            }
            return false;
            
            

            
        }
        public bool Stop()
        {

            byte[] command1 = new byte[3] ;
           
            command1[0] = (byte)'R';
            command1[1] = (byte)'A';
            command1[2] = (byte)'e';

            if (errorstate == false)
            {
                if (b_tooth.sendData(command1, 0, 3)) return true;
                else return false;
            }
            return false;

        }
        public bool Go()
        {

            byte[] command1 = new byte[3];

            command1[0] = (byte)'R';
            command1[1] = (byte)'a';
            command1[2] = (byte)'e';

            if (errorstate == false)
            {
                if (b_tooth.sendData(command1, 0, 3)) return true;
                else return false;
            }
            return false;
           
        }


        public bool setSpeed(byte[] speed)
        {

            byte[] command = new byte[2];
            command[0] = (byte)'S';
            command[1] = speed[0];

            if (errorstate == false)
            {
                if (b_tooth.sendData(command, 0, 2)) return true;
                else return false;
            }
            return false;
            
        }

        public static void Query(object stateinfo)
        {
            Trolley asyc_trolley = (Trolley)stateinfo;

            while (true)
            {
                Thread.Sleep(2);
                switch (asyc_trolley.ProcToDo)
                {
                    case ProcNameTrolley.FORWARD:
                        //Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
                        asyc_trolley.Forward();
                        //Thread.CurrentThread.Priority = ThreadPriority.Normal;
                        asyc_trolley.proc_to_do = ProcNameTrolley.IDLE;
                        break;
                    case ProcNameTrolley.REVERSE:
                        //Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
                        asyc_trolley.Reverse();
                        //Thread.CurrentThread.Priority = ThreadPriority.Normal;
                        asyc_trolley.proc_to_do = ProcNameTrolley.IDLE;
                        break;
                    case ProcNameTrolley.GO:
                        //Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
                        asyc_trolley.Go();
                        //Thread.CurrentThread.Priority = ThreadPriority.Normal;
                        asyc_trolley.proc_to_do = ProcNameTrolley.IDLE;
                        break;
                    case ProcNameTrolley.STOP:
                        //Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
                        asyc_trolley.Stop();
                        //Thread.CurrentThread.Priority = ThreadPriority.Normal;
                        asyc_trolley.proc_to_do = ProcNameTrolley.IDLE;
                        break;
                    case ProcNameTrolley.SETSPEED:
                        //Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
                        asyc_trolley.setSpeed(asyc_trolley.SpeedByte);
                        //Thread.CurrentThread.Priority = ThreadPriority.Normal;
                        asyc_trolley.proc_to_do = ProcNameTrolley.IDLE;
                        break;
                    case ProcNameTrolley.IDLE:
                        Thread.Sleep(2);
                        break;

                }
                
                
            }
        }

    }
}
