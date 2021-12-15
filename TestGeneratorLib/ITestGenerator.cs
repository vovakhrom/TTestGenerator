using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestGeneratorLib.Info;

namespace TestGeneratorLib
{
    public interface ITestGenerator
    {
        public Dictionary<string, string> GenerateTests(FileInfo fileInfo);
    }
}