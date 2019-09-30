using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Trolley_Control
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Tunnel_Control_Form tunnel = new Tunnel_Control_Form();
            Application.ApplicationExit += new EventHandler(tunnel.Application_ApplicationExit);

            Application.Run(tunnel);
        }
    }
}
