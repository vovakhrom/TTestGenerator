using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestGeneratorLib.Info
{
    public class ConstructorInfo
    {
        public string Name { get; private set; }
        public Dictionary<string, string> Parameters { get; private set; }

        public ConstructorInfo(Dictionary<string, string> parameters, string name)
        {
            this.Name = name;
            this.Parameters = parameters;
        }
    }
}
