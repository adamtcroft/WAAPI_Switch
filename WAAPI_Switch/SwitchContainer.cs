using System;
using System.Collections.Generic;
namespace WAAPI_Switch
{
    public class SwitchContainer
    {
        public string name { get; set; }
        public string id { get; set; }
        public List<SwitchAssignment> assignments = new List<SwitchAssignment>();
        public List<SwitchContainerChild> children = new List<SwitchContainerChild>();
    }

    public class SwitchAssignment
    {
        public string child { get; set; }
        public string stateOrSwitch { get; set; }
    }

    public class SwitchContainerChild
    {
        public string name { get; set; }
        public string id { get; set; }
    }
}
