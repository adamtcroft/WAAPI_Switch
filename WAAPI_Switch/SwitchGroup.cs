using System;
using System.Collections.Generic;
namespace WAAPI_Switch
{
    public class SwitchGroup
    {
        public SwitchGroup()
        {
        }

        public string name { get; set; }
        public string id { get; set; }
        public List<WwiseSwitch> switches = new List<WwiseSwitch>();
    }
}
