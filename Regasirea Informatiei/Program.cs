using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using WordFrequency;

namespace Regasirea_Informatiei
{
    class Program
    {
        public static List<string> uniqueGlobalWords = new List<string>();
        public static Dictionary<int, Dictionary<int, int>> wordFrequency = new Dictionary<int, Dictionary<int, int>>();
        static void Main(string[] args)
        {
            string projectPath = Environment.CurrentDirectory;
            string resourcesPath = projectPath + "\\Reuters\\Reuters_34\\Training";
            string[] files = Directory.GetFiles(resourcesPath);
            //printFiles(files);
            readFiles(files);
            printGlobalWord();
        }

        private static void printGlobalWord()
        {
            foreach (string word in uniqueGlobalWords)
            {
                Console.WriteLine(word);
            }
        }

        private static void readFiles(string[] files)
        {
            //int ctFisier = 0;
            foreach (string file in files)
            {
                try
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load(file);
                    XmlNodeList textNode = xmlDoc.GetElementsByTagName("text");
                    XmlNodeList titleNode = xmlDoc.GetElementsByTagName("title");
                    string textWords = null;
                    string titleWords = null;
                    foreach (XmlNode node in titleNode)
                    {
                        textWords = node.InnerText;
                    }
                    foreach (XmlNode node in textNode)
                    {
                        titleWords = node.InnerText;
                    }
                    verifyIfStopWordAndExtractRoots(textWords, titleWords);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private static void verifyIfStopWordAndExtractRoots(string textWords, string titleWords)
        {
            string[] titleWordsSplitted = textWords.Split(' ');
            string[] textWordsSplitted = titleWords.Split(' ');
            List<string> words = new List<string>();
            string currentSolutionPath = Environment.CurrentDirectory;
            string fileName = "\\stopwords.txt";
            string[] stopWords = readStopWords(currentSolutionPath + fileName);

            foreach (string word in titleWordsSplitted)
            {
                words.Add(word);
            }
            foreach (string word in textWordsSplitted)
            {
                words.Add(word);
            }
            foreach (string word in words)
            {
                if (stopWords.Contains(word))
                {
                    //don't add it
                    return;
                }
                else
                {
                    PorterStemmer porterStemmer = new PorterStemmer();
                    string cleanedWord = porterStemmer.StemWord(word);
                    cleanedWord = Regex.Replace(cleanedWord, @"[^a-zA-Z0-9]", "").ToLower();
                    if (cleanedWord == "" || ContainsOnlyNumbers(cleanedWord)) 
                    {
                        //|| ContainsOnlyNumbers(cleanedWord)
                        continue;
                    }
                    if (!uniqueGlobalWords.Contains(cleanedWord))
                    {
                        uniqueGlobalWords.Add(cleanedWord);
                        Console.WriteLine(cleanedWord);
                    }
                }
            }
        }

        private static bool ContainsOnlyNumbers(string cleanedWord)
        {
            foreach (char c in cleanedWord)
            {
                if (!char.IsDigit(c))
                {
                    return false;
                }
            }
            return true;
        }

        private static string[] readStopWords(string path)
        {
            try
            {
                string stopWords = File.ReadAllText(path);
                return stopWords.Split('\n');
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message + "null value!");
                return null;
            }
        }
    }
}
