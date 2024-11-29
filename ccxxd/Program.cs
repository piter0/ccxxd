using System.Text;

namespace ccxxd
{
    public class Program
    {
        private static readonly string OptionOctetsPerLine = "-c";
        private static readonly string OptionLittleEndian = "-e";
        private static readonly string OptionGroupSize = "-g";
        private static readonly string OptionLen = "-l";
        private static readonly string OptionRevert = "-r";
        private static readonly string OptionSeekOffset = "-s";
        private static readonly int LittleEndianSize = 4;
        private static readonly int DefaultGroupSize = 2;
        private static readonly int DefaultOctetsPerLine = 16;
        private static readonly int DefaultSeekOffset = 0;

        private static readonly HashSet<string> OptionsSet =
        [
            OptionOctetsPerLine, OptionLittleEndian, OptionGroupSize,
            OptionLen, OptionRevert, OptionSeekOffset
        ];

        static void Main(string[] args)
        {
            try
            {
                ValidateArguments(args);
                var parsedArgs = ParseArguments(args);
                var filePath = args[0];

                if (parsedArgs.ContainsKey(OptionRevert))
                {
                    RevertFile(filePath);
                }
                else
                {
                    ProcessFile(filePath, parsedArgs);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e}");
                PrintUsage();
            }
        }

        private static void RevertFile(string filePath)
        {
            if (!filePath.EndsWith(".hex"))
            {
                throw new ArgumentException("Revert operation requires a .hex file");
            }

            byte[] buffer = File.ReadAllBytes(filePath);

            using var output = new BinaryWriter(File.Open("reverted-file.bin", FileMode.Create));
            foreach (var line in Encoding.ASCII.GetString(buffer).Split("\r\n"))
            {
                if (line.Length < 57)
                {
                    continue;
                }

                var bytesPart = line[10..57].Trim().Split(' ');
                foreach (var hexByte in bytesPart)
                {
                    if (!string.IsNullOrWhiteSpace(hexByte))
                    {
                        output.Write(Convert.ToByte(hexByte, 16));
                    }
                }
            }
        }

        private static void ProcessFile(string filePath, Dictionary<string, int> parsedArgs)
        {
            var fs = new FileStream(filePath, FileMode.Open);

            var groupSize = GetGroupSizeInBytes(parsedArgs);
            var seekOffset = GetSeekOffset(parsedArgs);
            var octetsLength = seekOffset + GetOctetsLength(parsedArgs) ?? (int)fs.Length;
            var octetsPerLine = GetOctetsPerLine(parsedArgs);

            var buffer = new byte[octetsLength];

            fs.Seek(seekOffset, SeekOrigin.Begin);
            fs.Read(buffer, 0, octetsLength);

            for (var i = seekOffset; i < octetsLength; i += octetsPerLine)
            {
                Console.Write(i.ToString("x8") + ": ");
                Console.Write(PrintDataAsHex(buffer[i..Math.Min(i + octetsPerLine, octetsLength)], groupSize));
                Console.Write(PrintDataAsText(buffer[i..Math.Min(i + octetsPerLine, octetsLength)], i + octetsPerLine - octetsLength));
            }
        }

        private static int GetSeekOffset(Dictionary<string, int> parsedArgs)
            => parsedArgs.TryGetValue(OptionSeekOffset, out var value) ? value : DefaultSeekOffset;

        private static int GetOctetsPerLine(Dictionary<string, int> parsedArgs)
            => parsedArgs.TryGetValue(OptionOctetsPerLine, out var value) ? value : DefaultOctetsPerLine;

        private static int? GetOctetsLength(Dictionary<string, int> parsedArgs)
            => parsedArgs.TryGetValue(OptionLen, out var value) ? value : null;

        private static int GetGroupSizeInBytes(Dictionary<string, int> parsedArgs)
        {
            if (parsedArgs.ContainsKey(OptionGroupSize))
            {
                return parsedArgs.GetValueOrDefault(OptionGroupSize);
            }
            else if (parsedArgs.ContainsKey(OptionLittleEndian))
            {
                return LittleEndianSize;
            }

            return DefaultGroupSize;
        }

        private static Dictionary<string, int> ParseArguments(string[] args)
        {
            var arguments = new Dictionary<string, int>();
            for (var i = 1; i < args.Length; i++)
            {
                var option = args[i];
                if (!OptionsSet.Contains(option))
                {
                    throw new ArgumentException($"Invalid option: {option}");
                }

                if (option == OptionLittleEndian || option == OptionRevert)
                {
                    arguments[option] = 0;
                }
                else if (i + 1 < args.Length && int.TryParse(args[i + 1], out int value))
                {
                    arguments[option] = value;
                    i++;
                }
                else
                {
                    throw new ArgumentException($"Missing or invalid value for option: {option}");
                }
            }
            return arguments;
        }

        private static string PrintDataAsHex(byte[] buffer, int groupSize)
        {
            return string.Join("", buffer
                .Select((b, i) => $"{b:x2}" + (i % groupSize == groupSize - 1 ? " " : string.Empty))
                .ToArray());
        }

        private static string PrintDataAsText(byte[] buffer, int padLeft)
        {
            var ascii = new string(buffer.Select(c => char.IsControl((char)c) ? '.' : (char)c).ToArray());
            var padding = padLeft > 0 ? new string(' ', (int)Math.Ceiling(padLeft * 2.5)) : string.Empty;

            return $"{padding} {ascii}\n";
        }

        private static void ValidateArguments(string[] args)
        {
            if (args.Length == 0)
            {
                throw new ArgumentException($"Type at least one parameter");
            }
            if (!File.Exists(args[0]))
            {
                throw new FileNotFoundException($"Cannot open file: {args[0]}");
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: ccxxd <file path> [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("  -c <value>  Set octets per line.");
            Console.WriteLine("  -e          Use little-endian format.");
            Console.WriteLine("  -g <value>  Group bytes (default: 2).");
            Console.WriteLine("  -l <value>  Limit length.");
            Console.WriteLine("  -r          Revert hex to binary.");
            Console.WriteLine("  -s <value>  Start at seek offset.");
        }
    }
}
