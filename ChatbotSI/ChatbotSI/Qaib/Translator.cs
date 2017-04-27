using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatbotSI
{
    //TODO change to static constructor for mild optimization purposes
    public static class Translator
    {
        static bool loaded = false;

        static int _symbolCount;
        public static int SymbolCount
        {
            get
            {
                if (!loaded)
                    Load();

                return _symbolCount;
            }

        }

        static byte[] char2symbol;
        static char[] symbol2char;

        static void Load()
        {
            //Create known alphabet of symbols
            List<char> symbols = new List<char>();

            //Add void char
            symbols.Add('_');

            //Add punctuation
            symbols.Add(' ');
            symbols.Add('.');
            symbols.Add(',');
            symbols.Add('!');
            symbols.Add('?');

            //add numbers
            //for (int i = 0; i < 10; i++)
            //    symbols.Add((char)((byte)'0' + (byte)i));

            //add lower-case letters
            for (int i = 0; i < 26; i++)
                symbols.Add((char)((byte)'a' + (byte)i));

            //add upper-case letters
            //for (int i = 0; i < 26; i++)
            //    symbols.Add((char)((byte)'A' + (byte)i));


            //Add default unknown char as the last symbol
            symbols.Add('#');

            //populate symbol to char translation table
            symbol2char = new char[symbols.Count];
            for (int i = 0; i < symbols.Count; i++)
            {
                symbol2char[i] = symbols[i];
            }

            //populate char to symbol table
            char2symbol = new byte[256];
            for (int i = 0; i < 256; i++)
            {
                //default unknown case 
                byte translation = (byte)(symbols.Count - 1);

                //find translation for i
                for (int k = 0; k < symbols.Count; k++)
                    if (i == (int)symbols[k])
                        translation = (byte)k; //translates to order in list

                //set table
                char2symbol[i] = translation;
            }

            //Set Uppercase chars to same symbol as lowecase chars
            //Find lower a symbol index
            int aIndex = 0;
            for (int i = 0; i < symbols.Count; i++)
                if (symbols[i] == (byte)'a')
                    aIndex = i;

            //set upper case chars to lower cases symbols
            for (int i = 0; i < 26; i++)
                char2symbol[((byte)'A') + i] = (byte)(aIndex + i);

            _symbolCount = symbols.Count;
            Console.WriteLine("Symbol library created with " + _symbolCount + " symbols");
            loaded = true;
        }

        public static byte textToSymbol(char input)
        {
            if (!loaded)
                Load();

            return char2symbol[(byte)input];
        }
        public static byte[] textToSymbol(string input)
        {
            byte[] symbols = new byte[input.Length + 1];
            for (int i = 0; i < input.Length; i++)
            {
                symbols[i] = textToSymbol(input[i]);
            }
            symbols[symbols.Length - 1] = textToSymbol('_');
            return symbols;
        }
        public static SymbolDialogue textToSymbol(Corpus.Dialogue text)
        {
            SymbolDialogue symbols = new SymbolDialogue();

            symbols.sentences = new SymbolSentence[text.entrances.Length];
            for (int i = 0; i < symbols.sentences.Length; i++)
            {
                symbols.sentences[i] = new SymbolSentence();
                symbols.sentences[i].symbols = textToSymbol(text.entrances[i]);
            }

            return symbols;
        }
        public static SymbolCorpus textToSymbol(Corpus text)
        {
            SymbolCorpus symbols = new SymbolCorpus();

            symbols.dialogues = new SymbolDialogue[text.dialogues.Length];
            for (int i = 0; i < symbols.dialogues.Length; i++)
                symbols.dialogues[i] = textToSymbol(text.dialogues[i]);

            return symbols;
        }

        public static char symbolToText(byte symbol)
        {
            if (!loaded)
                Load();

            if (symbol < symbol2char.Length)
                return symbol2char[symbol];
            else
                return symbol2char[symbol2char.Length - 1];
            return symbol2char[symbol];
        }
        public static string symbolToText(byte[] symbols)
        {
            string output = "";
            for (int i = 0; i < symbols.Length; i++)
            {
                output += symbolToText(symbols[i]);
            }

            if (output[output.Length - 1] == '_')
                output = output.Substring(0, output.Length - 1);
            return output;
        }
        public static Corpus.Dialogue symbolToText(SymbolDialogue symbols)
        {
            Corpus.Dialogue dialogue = new Corpus.Dialogue();
            dialogue.entrances = new string[symbols.sentences.Length];

            for (int i = 0; i < dialogue.entrances.Length; i++)
            {
                dialogue.entrances[i] = symbolToText(symbols.sentences[i].symbols);
            }

            return dialogue;
        }
        public static Corpus symbolToText(SymbolCorpus symbols)
        {
            Corpus text = new Corpus();
            text.dialogues = new Corpus.Dialogue[symbols.dialogues.Length];

            for(int i=0;i<text.dialogues.Length;i++)
            {
                text.dialogues[i] = symbolToText(symbols.dialogues[i]);
            }

            return text;
        }
    }
}
