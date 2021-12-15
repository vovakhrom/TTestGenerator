using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestGeneratorLib;

namespace MainPart
{
    class Program
    {
        static void Main(string[] args)
        {
             var pathToFolder = "D:\\спп 4\\TestGenerator-master\\MainPart\\Files";
            var pathToGenerated = @"D:\\спп 4\\TestGenerator-master\\GeneratedTests\\GeneratedFiles";
            
            if (!Directory.Exists(pathToFolder))
            {
                Console.WriteLine($"Couldn't find directory {pathToFolder}");
                return;
            }
            if (!Directory.Exists(pathToGenerated))
            {
                Directory.CreateDirectory(pathToGenerated);
            }

            var allFiles = Directory.GetFiles(pathToFolder);

            var files = from file in allFiles
                    where file.Substring(file.Length - 3) == ".cs"
                    select file;
            ITestGenerator generato = new TestsGenerator();
            Task task =  new Pipeline().Generate(files, pathToGenerated,  generato);
            task.Wait();
            Console.WriteLine("end.");
        }
    }
}
