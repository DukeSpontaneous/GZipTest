using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using GZipTest.Helpers;

namespace GZipTest
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                ProcessingCommand(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка на этапе подготовки ресурсов: {0}{1}", Environment.NewLine, ex.Message);
            }
        }

        static void ProcessingCommand(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Ошибка: передано недостаточно параметров!");
                Console.WriteLine(Messages.CMD_HELP);
                return;
            }

            string cmd = args[0].ToLower();
            string source = args[1];
            string distination = args[2];

            switch (cmd)
            {
                case "compress":
                    Console.WriteLine(Messages.CMD_WAITING);
                    GZipHelper.ChunkedCompress(source, distination);
                    break;
                case "decompress":
                    Console.WriteLine(Messages.CMD_WAITING);
                    GZipHelper.ChunkedDecompress(source, distination);
                    break;
                default:
                    Console.WriteLine("Ошибка: запрошено выполнение нереализованной операции!");
                    Console.WriteLine(Messages.CMD_HELP);
                    break;
            }
        }

    }
}
