using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Trolley_Control
{
    class PTU303:Barometer
    {

        public PTU303(ref VaisalaUpdateGui pbarug)
        {
            try
            {
                throw new NotImplementedException();
            }
            catch (NotImplementedException)
            {
                MessageBox.Show("PTU303 has not been implemented yet");

            }
        }

        public override void Close()
        {
            try
            {
                throw new NotImplementedException();
            }
            catch (NotImplementedException)
            {
                MessageBox.Show("PTU303 has not been implemented yet");

            }
         
        }

        public override bool IsOpen()
        {
            try
            {
                throw new NotImplementedException();
            }
            catch (NotImplementedException)
            {
                MessageBox.Show("PTU303 has not been implemented yet");
                return false;
            }
            
        }

        public override double getPressure()
        {
            return result + current_correction;
        }


    }
}
