using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetWebCam2
{
    public class DetectedLogo
    {
        public string class_id;
        public string class_name;
        public double confidence;
        public List<int> box = new List<int>();
        public double scale;

    }
}
