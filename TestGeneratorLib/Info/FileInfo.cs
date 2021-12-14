using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestGeneratorLib.Info
{
    public class FileInfo
    {
        public List<ClassInfo> Classes { get; private set; }

        public FileInfo(List<ClassInfo> classes)
        {
            this.Classes = classes;
        }
    }
}
