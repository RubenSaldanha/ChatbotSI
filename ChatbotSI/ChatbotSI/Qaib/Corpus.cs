using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatbotSI
{
    public class Corpus
    {
        public string name;

        public Dialogue[] dialogues;

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

        public string toString()
        {
            string text = "";
            for(int i=0;i<dialogues.Length;i++)
            {
                text += "Dialogue " + i + " :\n";

                for(int j=0;j<dialogues[i].entrances.Length;j++)
                {
                    text += dialogues[i].entrances[j];
                    text += "\n";
                }
            }

            return text;
        }

        public Corpus subCorpus(double percentage, int seed)
        {
            Random rdm = new Random(seed);

            int dialogueCount = 1 + (int)(percentage * dialogues.Length);

            Corpus subCorp = new Corpus();
            subCorp.dialogues = new Dialogue[dialogueCount];

            List<int> excluded = new List<int>();
            for (int i = 0; i < dialogues.Length; i++)
                excluded.Add(i);

            int includeIndex;
            for (int i = 0; i < subCorp.dialogues.Length; i++)
            {
                includeIndex = rdm.Next(excluded.Count);
                subCorp.dialogues[i] = dialogues[excluded[includeIndex]];
                excluded.RemoveAt(includeIndex);
            }

            return subCorp;
        }

        public class Dialogue
        {
            public string[] entrances;

            public int getCharCount()
            {
                int count = 0;

                for (int i = 0; i < entrances.Length; i++)
                    count += entrances[i].Length;

                return count;
            }
            public int getSentenceCount()
            {
                return entrances.Length;
            }
        }

        public static Corpus loadCorpus(string file)
        {
            string[] lines = System.IO.File.ReadAllLines(file);
            List<byte> info = new List<byte>();

            List<Dialogue> dialogues = new List<Dialogue>();
            List<string> currentDialogue = new List<string>();

            //weak parser
            Dialogue dialogueToAdd;
            for (int i = 1; i < lines.Length; i++)
            {
                if (lines[i] == "")
                {
                    //Conversation over
                    dialogueToAdd = new Dialogue();
                    dialogueToAdd.entrances = currentDialogue.ToArray();
                    dialogues.Add(dialogueToAdd);

                    currentDialogue.Clear();

                    i++; //skip "Conversation"
                    continue;
                }
                else
                {
                    currentDialogue.Add(lines[i]);
                }
            }

            if(currentDialogue.Count != 0)
            {
                dialogueToAdd = new Dialogue();
                dialogueToAdd.entrances = currentDialogue.ToArray();
                dialogues.Add(dialogueToAdd);
            }

            Console.WriteLine("Conversations Loaded: " + dialogues.Count);

            Corpus loaded = new Corpus();
            loaded.dialogues = dialogues.ToArray();
            loaded.name = Path.GetFileName(file);
            return loaded;
        }
        public static Corpus getCornwell()
        {
            return loadCorpus("cornwell.txt");
        }
    }
}
