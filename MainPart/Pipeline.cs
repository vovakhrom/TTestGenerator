using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TestGeneratorLib;

namespace MainPart
{
    public class Pipeline
    {

        public int LOAD_LIMIT = 1;
        public int GENERATE_LIMIT = 1;
        public int WRITE_LIMIT = 1 ;
        int i =  0;
        public Task Generate(IEnumerable<string> files, string pathToGenerated, ITestGenerator generato)
        {
            var loadOptions = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = LOAD_LIMIT };
            var generateOptions = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = GENERATE_LIMIT };
            var writeOptions = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = WRITE_LIMIT };

            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
            //на вход строка путь к файлу и возвращает string как целиком прочитанный файл
            var loadBlock = new TransformBlock<string, string>
            (
                async path =>
                {
                    using (var reader = new StreamReader(path))
                    {
                        Console.WriteLine("Load");

                        return await reader.ReadToEndAsync();
                    }
                },
                loadOptions
            );
            
            var generateBlock = new TransformManyBlock<string, KeyValuePair<string, string>>
            (
                async sourceCode =>
                {
                    Thread.Sleep(2000);
                    i++;
                    if (i == 3)
                    {
                        throw new Exception();
                    }
                    Console.WriteLine("Generating");
                    
                    var fileInfo = await Task.Run(() => CodeAnalyzer.GetFileInfo(sourceCode));
                    return await Task.Run(() => generato.GenerateTests(fileInfo));
                },
                generateOptions
            );

            var writeBlock = new ActionBlock<KeyValuePair<string, string>>
            (
                async fileNameCodePair =>
                {
                    Console.WriteLine("Writing");
                    using (var writer = new StreamWriter(pathToGenerated + '\\' + fileNameCodePair.Key + ".cs"))
                    {
                        await writer.WriteAsync(fileNameCodePair.Value);
                        Console.WriteLine(fileNameCodePair.Key);
                    }
                },
                writeOptions
            );

            loadBlock.LinkTo(generateBlock, linkOptions);
            generateBlock.LinkTo(writeBlock, linkOptions);

            foreach (var file in files)
            {
                loadBlock.Post(file);
            }

            loadBlock.Complete();

            return writeBlock.Completion;
        }
    }
}
