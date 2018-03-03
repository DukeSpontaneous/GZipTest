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
                Console.WriteLine("Ошибка: {0}", ex.Message);
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
                    GZipHelper.ChunkedCompress(source, distination);
                    Console.WriteLine(Messages.CMD_SUCCESS);
                    break;
                case "decompress":
                    GZipHelper.ChunkedDecompress(source, distination);
                    Console.WriteLine(Messages.CMD_SUCCESS);
                    break;
                default:
                    Console.WriteLine("Ошибка: запрошено выполнение нереализованной операции!");
                    Console.WriteLine(Messages.CMD_HELP);
                    break;
            }
        }

    }
}
