using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using TestGeneratorLib;

namespace MainPart
{
    public class Pipeline
    {

        public int LOAD_LIMIT = 10;
        public int GENERATE_LIMIT = 1;
        public int WRITE_LIMIT = 1 ;

        public Task Generate(IEnumerable<string> files, string pathToGenerated)
        {
            var loadOptions = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = LOAD_LIMIT };
            var generateOptions = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = GENERATE_LIMIT };
            var writeOptions = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = WRITE_LIMIT };

            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

            var loadBlock = new TransformBlock<string, string>
            (
                async path =>
                {
                    Console.WriteLine("read-S");
                    using (var reader = new StreamReader(path))
                    {
                        Console.WriteLine("read-F");
                        return await reader.ReadToEndAsync();
                    }
                },
                loadOptions
            );

            var generateBlock = new TransformManyBlock<string, KeyValuePair<string, string>>
            (
                async sourceCode =>
                {
                    Console.WriteLine("generate-S");
                    var fileInfo = await Task.Run(() => CodeAnalyzer.GetFileInfo(sourceCode));
                    Console.WriteLine("generate-F");
                    return await Task.Run(() => TestsGenerator.GenerateTests(fileInfo));
                },
                generateOptions
            );

            var writeBlock = new ActionBlock<KeyValuePair<string, string>>
            (
                async fileNameCodePair =>
                {
                    Console.WriteLine("write-S " + fileNameCodePair.Key);
                    using (var writer = new StreamWriter(pathToGenerated + '\\' + fileNameCodePair.Key + ".cs"))
                    {
                        await writer.WriteAsync(fileNameCodePair.Value);
                        Console.WriteLine("write-F " + fileNameCodePair.Key);
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
