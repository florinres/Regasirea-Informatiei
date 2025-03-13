using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Regasirea_Informatiei
{
    class WordProcessor : Words
    {
        internal void processFiles(string[] files)
        {
            foreach(string file in files){
                try
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load(file);
                    XmlNodeList textNodes = xmlDoc.GetElementsByTagName("text");

                    foreach (XmlNode textNode in textNodes)
                    {
                        parseText(textNode.InnerText);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error processing file: " + file);
                    Console.WriteLine(e.Message);
                }
            }
        }
    }
}
