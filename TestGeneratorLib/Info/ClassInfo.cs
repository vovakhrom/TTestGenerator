using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestGeneratorLib.Info
{
    public class ClassInfo
    {
        public List<MethodInfo> Methods { get; private set; }
        public string ClassName { get; private set; }
        public List<ConstructorInfo> Constructors { get; private set; }

        public ClassInfo(List<MethodInfo> methods, List<ConstructorInfo> constructors, string className)
        {
            this.Methods = methods;
            this.Constructors = constructors;
            this.ClassName = className;
        }
    }
}
