using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Regasirea_Informatiei
{
    class Words
    {
        private List<string> words;
        
        public Words()
        {
            words = new List<string>();
        }

        public void parseText(string text)
        {
            foreach (string word in text.Split(' '))
            {
                if(words.Contains(word))
                {
                    return;
                }
                else
                {
                    words.Add(word);
                }
            }
        }   
    }
}
