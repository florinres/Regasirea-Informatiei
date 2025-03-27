using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
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
            string resourcesPath = projectPath + "\\Reuters\\Reuters_7083";
            string[] files = Directory.GetFiles(resourcesPath);

            readFiles(files);                  
            pruneInfrequentWords(minDocuments: 5); 
            printGlobalWord();                 
            saveToFile();                      
        }



        private static void pruneInfrequentWords(int minDocuments = 5)
        {
            Dictionary<string, int> wordDocumentCount = new Dictionary<string, int>();

            foreach (var fileEntry in vectorOfWords)
            {
                var wordIndices = fileEntry.Value.Keys;
                foreach (int wordIndex in wordIndices)
                {
                    string word = uniqueGlobalWords[wordIndex];
                    if (!wordDocumentCount.ContainsKey(word))
                        wordDocumentCount[word] = 0;
                    wordDocumentCount[word]++;
                }
            }

            var prunedWords = wordDocumentCount.Where(kvp => kvp.Value >= minDocuments)
                                              .Select(kvp => kvp.Key)
                                              .ToList();

            var indexMapping = new Dictionary<int, int>();
            for (int i = 0; i < uniqueGlobalWords.Count; i++)
            {
                if (prunedWords.Contains(uniqueGlobalWords[i]))
                {
                    indexMapping[i] = prunedWords.IndexOf(uniqueGlobalWords[i]);
                }
            }

            uniqueGlobalWords = prunedWords;
            var newVector = new Dictionary<int, Dictionary<int, int>>();

            foreach (var fileEntry in vectorOfWords)
            {
                int fileKey = fileEntry.Key;
                newVector[fileKey] = new Dictionary<int, int>();

                foreach (var kvp in fileEntry.Value)
                {
                    int oldWordIndex = kvp.Key;
                    if (indexMapping.ContainsKey(oldWordIndex))
                    {
                        int newWordIndex = indexMapping[oldWordIndex];
                        newVector[fileKey][newWordIndex] = kvp.Value;
                    }
                }
            }

            vectorOfWords = newVector;
            try
            {
                var rareWords = wordDocumentCount.Where(kvp => kvp.Value < minDocuments)
                               .Select(kvp => kvp.Key)
                               .ToList();
                File.WriteAllLines("rare_words.txt", rareWords); 
                Console.WriteLine($"Discarded {rareWords.Count} rare words.");
            }
            catch (SecurityException e)
            {
                throw e;
            }
        }

        private static void entropyTest()
        {
            List<double> procent = new List<double>();
            List<string> attributes = new List<string>();
            entropyReadFileWithAttributes(attributes, procent);
        }

        private static void entropyReadFileWithAttributes(List<string> attributes, List<double> procent)
        {
            string attributesPath = Environment.CurrentDirectory + "\\entropyTest.txt";
            try
            {
                string[] lines = File.ReadAllLines(attributesPath);
                foreach (string line in lines)
                {
                    if (line.Contains("@attribute"))
                    {
                        string[] splittedLine = line.Split(' ');
                        string attribute = splittedLine[1];
                        attributes.Add(attribute);
                        procent.Add(Double.Parse(splittedLine[2]));
                    }
                }
            }
            catch (FileNotFoundException e)
            {
                throw e;
            }
        }

        private static void saveToFile()
        {
            string currentDirectory = Environment.CurrentDirectory;
            string uniqueWordsFilePath = Path.Combine(currentDirectory, "uniqueGlobalWords.txt");
            string wordFrequencyFilePath = Path.Combine(currentDirectory, "vectorOfWords.txt");

            try
            {
                var formattedWords = uniqueGlobalWords.Select(word => $"@attribute {word}");
                File.WriteAllLines(uniqueWordsFilePath, formattedWords);
            }
            catch (IOException e)
            {
                Console.WriteLine($"Error writing to {uniqueWordsFilePath}: {e.Message}");
            }

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
                string lowerCaseWord = word.ToLower();
                string cleanedWord = porterStemmer.StemWord(lowerCaseWord);
                cleanedWord = Regex.Replace(cleanedWord, @"[^a-zA-Z]", "");

                if (cleanedWord.Length < 3 || cleanedWord.Length > 20 ||
                    stopWords.Contains(cleanedWord) ||
                    string.IsNullOrEmpty(cleanedWord) ||
                    ContainsOnlyNumbers(cleanedWord) ||
                    cleanedWord.EndsWith("ing") || cleanedWord.EndsWith("tion"))
                {
                    continue;
                }

                if (!uniqueGlobalWords.Contains(cleanedWord))
                {
                    uniqueGlobalWords.Add(cleanedWord);
                }
                addToWordFrequencyDictionary(cleanedWord, ctFisier);
                Console.WriteLine(cleanedWord);
            }
        }

        private static string removeNumbers(string cleanedWord)
        {
            bool numbersBefore = false;
            bool numbersAfter = false;

            foreach (char c in cleanedWord)
            {
                if (Char.IsDigit(c))
                {
                    numbersBefore = true;
                    break;
                }
            }
            for (char c = cleanedWord.Last(); c >= 0; c--)
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
            if (!vectorOfWords.ContainsKey(fileKey))
            {
                vectorOfWords[fileKey] = new Dictionary<int, int>();
            }
            var valuePairs = vectorOfWords[fileKey];
            int globalWordIndex = uniqueGlobalWords.IndexOf(cleanedWord);
            if (!valuePairs.ContainsKey(globalWordIndex))
            {
                valuePairs[globalWordIndex] = 1;
            }
            else
            {
                valuePairs[globalWordIndex]++;
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
