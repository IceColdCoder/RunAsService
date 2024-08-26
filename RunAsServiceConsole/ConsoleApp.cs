using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RunAsService
{
    class ConsoleApp
    {
        [STAThread]
        static void Main(string[] args)
        {
            var runAsService = new RunAsService();
            runAsService.Start(null);

            Console.ReadKey();
        }
    }
}
