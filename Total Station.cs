using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trolley_Control
{
    public class Total_Station:DUT
    {
        public Total_Station(ref DUTUpdateGui dutug_) : base(dutug_)
        {
            unit_timeout = 10000; //set to 10 s
            device_name = "Total Station";
        }

        public override bool Request(string request, ref string result)
        {
            return TCPClient.sendReceiveData(request, ref result);
        }
        public override void setTimeOut(int num_samples)
        {
            TCPClient.Timeout = unit_timeout * num_samples;
        }
    }
}
