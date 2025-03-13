using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Regasirea_Informatiei
{
    class Program
    {
        static void Main(string[] args)
        {
            string projectPath = Environment.CurrentDirectory;
            string resourcesPath = projectPath + "\\Reuters\\Reuters_34\\Training";
            string[] files = Directory.GetFiles(resourcesPath);
            printFiles(files);
            WordProcessor wordProcessor = new WordProcessor();
            wordProcessor.processFiles(files);
        }

        private static void printFiles(string[] files)
        {
            foreach (string file in files)
            {
                Console.WriteLine(file);
            }
        }
    }
}
