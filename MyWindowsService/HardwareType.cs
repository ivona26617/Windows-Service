using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyWindowsService
{
    class HardwareType
    {
        public int Id { get; set; }

        public string Model { get; set; }

        public string AdditionalInfo { get; set; }

        public HardwareType(int id, string model, string addinitialInfo)
        {
            this.Id = id;
            this.Model = model;
            this.AdditionalInfo = addinitialInfo;
        }

        public HardwareType()
        {
            
        }
    }
}
