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
        public const short BLUETOOTH_DATA = 102;
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
        Thread trolleythread;
        private List<short> execution_commands = new List<short>();
      

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
        public Thread TrolleyThread
        {
            get { return trolleythread; }
            set { trolleythread = value; }
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
                if (execution_commands.Count != 0) return execution_commands.First();  //return the oldest command in the list
                else return ProcNameTrolley.IDLE;
            }
            set
            {
                //limit the execution command list to 50 commands.  
                if (execution_commands.Count < 50)
                {
                    execution_commands.Add(value);
                }
                else
                {
                    //remove the oldest command before adding more commands
                    execution_commands.RemoveAt(0);
                    execution_commands.Add(value);
                }
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
            int max_list_size = 0;

            while (true)
            {
                asyc_trolley.trolleythread.Join(5);
                if (asyc_trolley.execution_commands.Count != 0)
                {
                    if (asyc_trolley.execution_commands.Count > max_list_size) max_list_size = asyc_trolley.execution_commands.Count;
                    asyc_trolley.proc_to_do = asyc_trolley.execution_commands.First();
                }
                else asyc_trolley.proc_to_do = ProcNameTrolley.IDLE;

                int count = Environment.TickCount & Int32.MaxValue;
                
                switch (asyc_trolley.proc_to_do)
                {
                    case ProcNameTrolley.FORWARD:
                        asyc_trolley.Forward();
                        asyc_trolley.execution_commands.RemoveAt(0);
                        continue;
                    case ProcNameTrolley.REVERSE:
                        asyc_trolley.Reverse();
                        asyc_trolley.execution_commands.RemoveAt(0);
                        continue;
                    case ProcNameTrolley.GO:
                        asyc_trolley.Go();
                        asyc_trolley.execution_commands.RemoveAt(0);
                        continue;
                    case ProcNameTrolley.STOP:
                        asyc_trolley.Stop();
                        asyc_trolley.execution_commands.RemoveAt(0);
                        continue;
                    case ProcNameTrolley.SETSPEED:
                        asyc_trolley.setSpeed(asyc_trolley.SpeedByte);
                        asyc_trolley.execution_commands.RemoveAt(0);
                        continue;
                    case ProcNameTrolley.IDLE:
                        //asyc_trolley.execution_commands.RemoveAt(0);
                        asyc_trolley.trolleythread.Join(3);
                        break;
                    case 0:
                        asyc_trolley.execution_commands.RemoveAt(0);
                        break;

                }
                
                
            }
        }

    }
}
