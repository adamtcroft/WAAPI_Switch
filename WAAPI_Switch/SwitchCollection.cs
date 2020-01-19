using System;
using System.Collections.Generic;

namespace WAAPI_Switch
{
    public class SwitchCollection
    {
        public SwitchCollection()
        {
        }

        public List<SwitchContainer> containers { get; set; }
        public List<SwitchGroup> groups { get; set; }
    }
}
