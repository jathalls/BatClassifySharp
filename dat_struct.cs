using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatClassifySharp
{
    internal class RowData
    {
        public string label = "NA";
        public string factor = "NA";
        public List<float> values = new List<float>();

        public RowData() { }
    }
}
