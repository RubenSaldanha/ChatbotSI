using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatbotSI
{
    public class SymbolCorpus
    {
        public SymbolDialogue[] dialogues;

        public SymbolCorpus()
        {

        }

        public int getCharCount()
        {
            int count = 0;

            for (int i = 0; i < dialogues.Length; i++)
                count += dialogues[i].getCharCount();

            return count;
        }
        public int getSentenceCount()
        {
            int count = 0;
            for (int i = 0; i < dialogues.Length; i++)
                count += dialogues[i].getSentenceCount();
            return count;
        }
        public int getDialogueCount()
        {
            return dialogues.Length;
        }


        public string getCorpusStats()
        {
            string stats = "";

            stats += "Dialogues: " + getDialogueCount() + "\n";
            stats += "Sentences: " + getSentenceCount() + "\n";
            stats += "Characters: " + getCharCount() + "\n";

            return stats;
        }
        public SymbolCorpus truncanteDialogues(int dialogueCount)
        {
            SymbolCorpus truncated = new SymbolCorpus();
            truncated.dialogues = new SymbolDialogue[dialogueCount];
            for(int i=0;i<truncated.dialogues.Length;i++)
            {
                truncated.dialogues[i] = dialogues[i];
            }

            return truncated;
        }
    }
    public class SymbolDialogue
    {
        public SymbolSentence[] sentences;
        public SymbolDialogue()
        {

        }
        
        public int getCharCount()
        {
            int count = 0;

            for (int i = 0; i < sentences.Length; i++)
                count += sentences[i].symbols.Length;

            return count;
        }
        public int getSentenceCount()
        {
            return sentences.Length;
        }
    }
    public class SymbolSentence
    {
        public byte[] symbols;
        public SymbolSentence()
        {

        }

    }

}
