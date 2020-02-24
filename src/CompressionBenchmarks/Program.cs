using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace CompressionBenchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            var testCount = 1;
            var testFile = "test.txt";

            GenerateFile(testFile);

            var testData = File.ReadAllBytes(testFile);

            int size = 0;
            ExecuteTest(testCount, "Copy", testData, (data) =>
            {
                var copy = new byte[data.Length];
                data.CopyTo(copy, 0);
                return copy.Length;
            });

            ExecuteTest(testCount, "ft.lz4", testData, (data) =>
            {
                using (var memoryStream = new MemoryStream(64 * 1024))
                {
                    using (var lz4 = FT.LZ4.LZ4Stream.Encode(memoryStream))
                    {
                        lz4.Write(data, 0, data.Length);
                        lz4.Flush();
                        return (int)memoryStream.Length;
                    }
                }
            });


            ExecuteTest(testCount, "Gzip", testData, (data) =>
            {
                using (var memoryStream = new MemoryStream(64 * 1024))
                {
                    using (var gz = new GZipStream(memoryStream, CompressionMode.Compress))
                    {
                        gz.Write(data, 0, data.Length);
                        gz.Flush();
                        return (int)memoryStream.Length;
                    }
                }
            });

            ExecuteTest(testCount, "deflate", testData, (data) =>
            {
                using (var memoryStream = new MemoryStream(64 * 1024))
                {
                    using (var deflate = new DeflateStream(memoryStream, CompressionMode.Compress))
                    {
                        deflate.Write(data, 0, data.Length);
                        deflate.Flush();
                        return (int)memoryStream.Length;
                    }
                }
            });

            /*
            ExecuteTest(testCount, "k40s", testData, (data) =>
            {
                using (var memoryStream = new MemoryStream(64 * 1024))
                {
                    using (var lz4 = K4os.Compression.LZ4.Streams.LZ4Stream.Encode(memoryStream))
                    {
                        lz4.Write(data, 0, data.Length);
                        lz4.Flush();
                        return (int)memoryStream.Length;
                    }
                }
            });
            */

            ExecuteTest(testCount, "lz4net", testData, (data) =>
            {
                using (var memoryStream = new MemoryStream(64 * 1024))
                {
                    using (var lz4 = new LZ4.LZ4Stream(memoryStream, CompressionMode.Compress))
                    {
                        lz4.Write(data, 0, data.Length);
                        lz4.Flush();
                        return (int)memoryStream.Length;
                    }
                }
            });



            Console.ReadKey();
        }

        private static void GenerateFile(string testFile)
        {
            var fileInfo = new FileInfo(testFile);

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            HashSet<string> words = new HashSet<string>();

            foreach (var assembly in assemblies)
            {
                AddWords(words, assembly.FullName);

                foreach (var type in assembly.GetTypes())
                {
                    AddWords(words, type.FullName);

                    foreach (var member in type.GetMembers())
                    {
                        AddWords(words, member.Name);
                    }
                }
            }

            var text = string.Join("", words.ToArray());

            while (!fileInfo.Exists || fileInfo.Length < 100 * 1024 * 1024)
            {
                File.AppendAllText(fileInfo.FullName, text);
                fileInfo.Refresh();
            }
        }

        private static void AddWords(HashSet<string> words, string fullName)
        {
            var items = fullName.Split('.', ',', ' ', '=', ';').Select(s => s.Trim()).Where(s => !String.IsNullOrEmpty(s)).ToArray();

            foreach (var item in items)
            {
                words.Add(item);
            }
        }

        private static void ExecuteTest(int count, string testName, byte[] data, Func<byte[], int> test)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var testCount = count;
            int size = 0;
            while (testCount > 0)
            {
                size = test(data);
                testCount--;
            }
            stopwatch.Stop();

            var testResult = new
            {
                testName,
                avg = TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds / (double)count),
                per = (int)(((double)size / (double)data.Length) * (Double)100)
            };

            Console.WriteLine(testResult);
        }
    }
}
