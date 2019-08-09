using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace dmpexe
{
    class Program
    {
        static int Main(string[] args)
        {
            var result = Parser.Default.ParseArguments<
                FilterOptions,
                Md5Options
                >(args).MapResult((FilterOptions o) => FilterRun(o),
                                  (Md5Options o) => Md5Run(o),
                                  error => 1
                );
            return result;
        }

        private static int Md5Run(Md5Options option)
        {
   
            foreach (var file in option.Files)
            {    
                var lines = File.ReadAllLines(file);
                var sb = new StringBuilder();
                int i = 0;
                foreach (var line in lines)
                {
                    if (!line.StartsWith("1")) continue;

                    var bytes = MD5.Create().ComputeHash(Encoding.ASCII.GetBytes(line));
                    var md5Line = ToHexStr(bytes);
                    sb.AppendLine(md5Line);
                    Console.WriteLine(i++);
                }

                File.WriteAllText(file + "md5.txt", sb.ToString());
            }
            return 0;
        }
        public static string ToHexStr(byte[] data, bool isUpperCase = false)
        {
            if (data == null)
                return null;

            var format = isUpperCase ? "{0:X2}" : "{0:x2}";
            var stringBuilder = new StringBuilder();
            foreach (var perByte in data)
                stringBuilder.AppendFormat(format, perByte);

            return stringBuilder.ToString();
        }

        private static int FilterRun(FilterOptions option)
        {
            foreach (var fn in option.Files)
            {
                Console.WriteLine($"正在处理:{fn}");
                var path = Path.GetDirectoryName(fn);
                var exists = (option.ValidateAll ? GetAllExists(path, fn) : new HashSet<string>());
                var lines = File.ReadAllLines(fn);
                HashSet<string> result = new HashSet<string>();
                foreach (var line in lines)
                {

                    if (string.IsNullOrWhiteSpace(line)) continue;
                    if (exists.Contains(line)) continue;
                    if (result.Contains(line)) continue;
                    if (line.Trim()[0] != '1') continue;
                    result.Add(line.Trim());
                    if (result.Count % 1000 == 0)
                    {
                        Console.WriteLine($"count:{result.Count}");
                    }

                    if (option.Max.HasValue && result.Count >= option.Max) break;
                }
                File.WriteAllLines(fn + "result.txt", result);
            }
            return 0;
        }
        private static HashSet<string> GetAllExists(string path, string fn)
        {
            var files = Directory.GetFiles(path);
            return files.Where(c => c != fn).Where(c => c.EndsWith("result.txt")).SelectMany(c => File.ReadAllLines(c)).ToHashSet();
        }
    }
    [Verb("md5", HelpText = "加密指定文件并存储为新文件")]
    public class Md5Options
    {
        [Value(0, Required = true, HelpText = "要处理的文件路径")]
        public IEnumerable<string> Files { get; set; }
    }
    [Verb("filter", HelpText = "过滤指定文件并存储为新文件")]
    public class FilterOptions
    {
        [Value(0, Required = true, HelpText = "要处理的文件路径")]
        public IEnumerable<string> Files { get; set; }

        [Option('m', "max", Required = false, HelpText = "新文件最大行数")]
        public int? Max { get; set; }

        [Option('v', "vali-all", Required = false, HelpText = "包含其它 result.txt文件验重", Default = false)]
        public bool ValidateAll { get; set; }

    }

}
