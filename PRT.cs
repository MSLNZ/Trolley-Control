using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Trolley_Control
{
    public class PRT
    {
        //note the report number is also used for prt identification
        private string prt_name;
        private string REPORT_NUMBER;
        private double A;
        private double B;
        private double R0;

        public PRT(string Report_number,double a, double b ,double r0)
        {
            REPORT_NUMBER = Report_number;
            A = a;
            B = b;
            R0 = r0;
        }
        public string getReportNumber()
        {
            return REPORT_NUMBER;
        }
        public double getA()
        {
            return A;
        }
        public double getB()
        {
            return B;
        }
        public double getR0()
        {
            return R0;
        }
        public string PRTName
        {
            set {prt_name = value;}
            get {return prt_name;}
        }

    }
}
