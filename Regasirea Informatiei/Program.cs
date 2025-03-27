using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using WordFrequency;

namespace Regasirea_Informatiei
{
    class Program
    {
        public static List<string> uniqueGlobalWords = new List<string>();
        public static Dictionary<int, Dictionary<int, int>> vectorOfWords = new Dictionary<int, Dictionary<int, int>>();
        static void Main(string[] args)
        {
            string projectPath = Environment.CurrentDirectory;
            string resourcesPath = projectPath + "\\Reuters\\Reuters_34\\Training";
            string[] files = Directory.GetFiles(resourcesPath);
            readFiles(files);
            printGlobalWord();
            saveToFile();
            saveWordAppearancesMatrix();
        }

        private static void saveToFile()
        {
            string currentDirectory = Environment.CurrentDirectory;
            string uniqueWordsFilePath = Path.Combine(currentDirectory, "uniqueGlobalWords.txt");
            string wordFrequencyFilePath = Path.Combine(currentDirectory, "vectorOfWords.txt");

            // Save uniqueGlobalWords to file
            try
            {
                File.WriteAllLines(uniqueWordsFilePath, uniqueGlobalWords);
            }
            catch (IOException e)
            {
                Console.WriteLine($"Error writing to {uniqueWordsFilePath}: {e.Message}");
            }

            // Save vectorOfWords to file
            try
            {
                using (StreamWriter writer = new StreamWriter(wordFrequencyFilePath))
                {
                    foreach (var fileEntry in vectorOfWords)
                    {
                        int fileKey = fileEntry.Key;
                        var wordFrequencies = fileEntry.Value;
                        string line = $"{fileKey}: [{string.Join(", ", wordFrequencies.Select(kvp => $"{kvp.Key}:{kvp.Value}"))}]";
                        writer.WriteLine(line);
                    }
                }
            }
            catch (IOException e)
            {
                Console.WriteLine($"Error writing to {wordFrequencyFilePath}: {e.Message}");
            }
        }

        private static void printGlobalWord()
        {
           Console.WriteLine("Unique global words: " + uniqueGlobalWords.Count);
           Console.WriteLine("Word frequency dictionary size: " + vectorOfWords.Count);
        }

        private static void readFiles(string[] files)
        {
            int fileKey = 0;
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
                    verifyIfStopWordAndExtractRoots(textWords, titleWords, fileKey);
                    fileKey++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private static void verifyIfStopWordAndExtractRoots(string textWords, string titleWords, int ctFisier)
        {
            string[] titleWordsSplitted = titleWords.Split(' ');
            string[] textWordsSplitted = textWords.Split(' ');
            string currentSolutionPath = Environment.CurrentDirectory + "\\stopwords.txt";
            HashSet<string> stopWords = new HashSet<string>(readStopWords(currentSolutionPath));

            PorterStemmer porterStemmer = new PorterStemmer();
            Regex regex = new Regex(@"[^a-zA-Z0-9]", RegexOptions.Compiled);

            foreach (string word in titleWordsSplitted.Concat(textWordsSplitted))
            {
                if (word.Length == 1)
                {
                    continue;
                }

                string cleanedWord = porterStemmer.StemWord(word.ToLower());
                cleanedWord = regex.Replace(cleanedWord, "");
                
                if (stopWords.Contains(word))
                {
                    continue;
                }

                if (string.IsNullOrEmpty(cleanedWord) || ContainsOnlyNumbers(cleanedWord))
                {
                    continue;
                }
                
                cleanedWord = removeNumbers(cleanedWord);

                if (!uniqueGlobalWords.Contains(cleanedWord))
                {
                    uniqueGlobalWords.Add(cleanedWord);
                    addToWordFrequencyDictionary(cleanedWord, ctFisier);
                }
            }
        }

        private static string removeNumbers(string cleanedWord)
        {
            bool numbersBefore = false;
            bool numbersAfter = false;

            foreach(char c in cleanedWord)
            {
                if(Char.IsDigit(c))
                {
                    numbersBefore = true;
                    break;
                }
            }
            for(char c = cleanedWord.Last(); c >= 0; c--)
            {
                if (Char.IsDigit(c))
                {
                    numbersAfter = true;
                    break;
                }
            }

            if (numbersBefore)
            {
                foreach (char c in cleanedWord)
                {
                    if (Char.IsLetter(c))
                    {
                        cleanedWord = cleanedWord.Substring(cleanedWord.IndexOf(c));
                        break;
                    }
                }
            }

            if (numbersAfter)
            {
                foreach (char c in cleanedWord.Reverse())
                {
                    if (Char.IsLetter(c))
                    {
                        cleanedWord = cleanedWord.Substring(0, cleanedWord.LastIndexOf(c) + 1);
                        break;
                    }
                }
            }

            return cleanedWord;
        }

        private static void addToWordFrequencyDictionary(string cleanedWord, int fileKey)
        {
            Dictionary<int, int> valuePairs = new Dictionary<int, int>();
            //if (!vectorOfWords.ContainsKey(fileKey))
            //{
            //    valuePairs = new Dictionary<int, int>();
            //    valuePairs.Add(uniqueGlobalWords.IndexOf(cleanedWord), 1);
            //    vectorOfWords.Add(fileKey, new Dictionary<int, int>());
            //    return;
            //}

            //valuePairs = vectorOfWords[fileKey];
            //if (valuePairs.ContainsValue(uniqueGlobalWords.IndexOf(cleanedWord)))
            //{
            //    return;
            //}
            //else
            //{
            //    valuePairs.Add(uniqueGlobalWords.IndexOf(cleanedWord), 1);
            //}
            //vectorOfWords[fileKey] = valuePairs;
            if (!vectorOfWords.ContainsKey(fileKey)){
                vectorOfWords.Add(fileKey, valuePairs);
            }
            valuePairs = vectorOfWords[fileKey];
            int globalWordIndex = uniqueGlobalWords.IndexOf(cleanedWord);
            if(!valuePairs.ContainsValue(globalWordIndex))
            {
                valuePairs.Add(globalWordIndex, 1);
                vectorOfWords[fileKey] = valuePairs;
            }
            else
            {
                return;
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
                return stopWords.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message + "null value!");
                return null;
            }
        }
        private static void saveWordAppearancesMatrix()
        {
            string currentDirectory = Environment.CurrentDirectory;
            string matrixFilePath = Path.Combine(currentDirectory, "wordAppearancesMatrix.txt");

            try
            {
                using (StreamWriter writer = new StreamWriter(matrixFilePath))
                {
                    // Write header
                    writer.Write("File\\Word");
                    foreach (var word in uniqueGlobalWords)
                    {
                        writer.Write($"\t{word}");
                    }
                    writer.WriteLine();

                    // Write each file's word appearance vector
                    foreach (var fileEntry in vectorOfWords)
                    {
                        int fileKey = fileEntry.Key;
                        var wordFrequencies = fileEntry.Value;
                        writer.Write($"F{fileKey}");

                        foreach (var word in uniqueGlobalWords)
                        {
                            int wordIndex = uniqueGlobalWords.IndexOf(word);
                            writer.Write($"\t{(wordFrequencies.ContainsKey(wordIndex) ? 1 : 0)}");
                        }
                        writer.WriteLine();
                    }
                }
            }
            catch (IOException e)
            {
                Console.WriteLine($"Error writing to {matrixFilePath}: {e.Message}");
            }
        }
    }
}
