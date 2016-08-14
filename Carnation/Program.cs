using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarnationNamespace
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = @"Carnation";

            if (args.Length == 0)
                DoWithoutArgs();
        }

        static void DoWithoutArgs()
        {
            string url = null;
            Carnation carnation = null;

            Console.Write("Url: ");
            url = Console.ReadLine();

            carnation = new Carnation(url);
            carnation.Run();
        }
    }
}
