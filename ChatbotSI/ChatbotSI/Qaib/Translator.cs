using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ChatbotSI
{
    //TODO change to static constructor for mild optimization purposes
    [Serializable()]
    [XmlInclude(typeof(CharToSymbolTranslator))]
    [XmlInclude(typeof(SyllableToSymbolTranslator))]
    public abstract class Translator
    {
        public abstract int getSymbolCount();

        public abstract byte[] textToSymbol(string input);
        public SymbolDialogue textToSymbol(Corpus.Dialogue text)
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
        public SymbolCorpus textToSymbol(Corpus text)
        {
            SymbolCorpus symbols = new SymbolCorpus();

            symbols.dialogues = new SymbolDialogue[text.dialogues.Length];
            for (int i = 0; i < symbols.dialogues.Length; i++)
                symbols.dialogues[i] = textToSymbol(text.dialogues[i]);

            return symbols;
        }

        public abstract string symbolToText(byte[] symbols);
        public Corpus.Dialogue symbolToText(SymbolDialogue symbols)
        {
            Corpus.Dialogue dialogue = new Corpus.Dialogue();
            dialogue.entrances = new string[symbols.sentences.Length];

            for (int i = 0; i < dialogue.entrances.Length; i++)
            {
                dialogue.entrances[i] = symbolToText(symbols.sentences[i].symbols);
            }

            return dialogue;
        }
        public Corpus symbolToText(SymbolCorpus symbols)
        {
            Corpus text = new Corpus();
            text.dialogues = new Corpus.Dialogue[symbols.dialogues.Length];

            for(int i=0;i<text.dialogues.Length;i++)
            {
                text.dialogues[i] = symbolToText(symbols.dialogues[i]);
            }

            return text;
        }

#warning word dictionary not ready
        public static void trs()
        {
            Dictionary<string, List<string>> dic = new Dictionary<string, List<string>>();
            List<string> synonims;

            synonims = new List<string>();
            synonims.Add("Hi");
            synonims.Add("Hello");
            synonims.Add("Hey");
            dic.Add("Hi", synonims);

            synonims = new List<string>();
            synonims.Add("cat");
            synonims.Add("feline");
            dic.Add("cat", synonims);
        }
    }

    [Serializable()]
    public class CharToSymbolTranslator : Translator
    {
        public bool loaded = false;

        public int _symbolCount;

        public byte[] char2symbol;
        public char[] symbol2char;

        void Load()
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
            symbols.Add('\'');

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

        public CharToSymbolTranslator()
        {
            Load();
        }

        public override int getSymbolCount()
        {
            return _symbolCount;
        }

        public byte textToSymbol(char input)
        {
            if (!loaded)
                Load();

            return char2symbol[(byte)input];
        }
        public override byte[] textToSymbol(string input)
        {
            byte[] symbols = new byte[input.Length + 1];
            for (int i = 0; i < input.Length; i++)
            {
                symbols[i] = textToSymbol(input[i]);
            }
            symbols[symbols.Length - 1] = textToSymbol('_');
            return symbols;
        }

        public char symbolToText(byte symbol)
        {
            if (!loaded)
                Load();

            if (symbol < symbol2char.Length)
                return symbol2char[symbol];
            else
                return symbol2char[symbol2char.Length - 1];
        }
        public override string symbolToText(byte[] symbols)
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
    }

    [Serializable()]
    public class SyllableToSymbolTranslator : Translator
    {
        public SyllableToSymbolTranslator()
        {

        }
        
        public override int getSymbolCount()
        {
            throw new NotImplementedException();
        }

        public override byte[] textToSymbol(string input)
        {
            throw new NotImplementedException();

            //Create symbol array with some length
            byte[] symbols;

            //TODO
            //Fill symbol array with symbols, based on input

            //Add last symbol as 0 = '_'
            symbols[symbols.Length - 1] = 0;
            return symbols;
        }

        public override string symbolToText(byte[] symbols)
        {
            throw new NotImplementedException();

            string output = "";

            //TODO
            //Fill output string based on symbols array (Don't forget last symbol is '_' = 0 )

            return output;
        }



        public static string normalize(string s)//Coverts a string to lower chars
        {
            s = s.ToLower();
            return s;
        }

        public static string simplificator(string s)//Removes consecutive chars, except 'e' and 'o'
        {
            char aux = '@';
            int pos = 0;

            foreach (char c in s)
            {
                if ((aux == c) & (c != 'e') & (c != 'o'))
                {
                    s = s.Remove(pos, 1);
                    pos--;
                    //System.Console.WriteLine("Removed "+c);
                }
                aux = c;
                pos++;

            }
            s = s + '.';
            return s;
        }

        public static List<string> soundificator(string s)
        {
            string vowels = "weyuioa";
            string trails = "rsflzcnm";
            string silents = "h";
            string consonants = "qtpdgjkxvb";
            string alphabet = "qwertyuiopasdfghjklzxcvbnm";
            string specials = " ";

            List<string> sounds = new List<string>();
            string temp = "";
            int current_is;
            int last_was = 0;//0 = undefined; 1 = vowel; 2 = trail; 3 = silent; 4 = consonant
            bool has_vowel = false;

            foreach (char c in s)
            {
                current_is = 0;
                if (!alphabet.Contains(c)) current_is = 0;
                if (vowels.Contains(c)) current_is = 1;
                if (trails.Contains(c)) current_is = 2;
                if (silents.Contains(c)) current_is = 3;
                if (consonants.Contains(c)) current_is = 4;

                switch (current_is)
                {
                    case (1):
                        temp = temp + c;
                        has_vowel = true;
                        last_was = 1;
                        break;
                    case (2):
                        switch (last_was)
                        {
                            case (1):
                                temp = temp + c;
                                break;
                            case (2):
                                if (has_vowel)
                                {
                                    sounds.Add(temp);
                                    //System.Console.WriteLine("Added: [{0}]", temp);
                                    has_vowel = false;
                                    temp = "" + c;
                                }
                                else
                                {
                                    temp = temp + c;
                                }
                                break;
                            case (3):
                                temp = temp + c;
                                break;
                            case (4):
                                temp = temp + c;
                                break;
                            case (0):
                                temp = "" + c;
                                break;
                        }
                        last_was = 2;
                        break;
                    case (3):
                        temp = temp + c;
                        last_was = 3;
                        break;
                    case (4):
                        switch (last_was)
                        {
                            case (1):
                                sounds.Add(temp);
                                //System.Console.WriteLine("Added: [{0}]", temp);
                                has_vowel = false;
                                temp = "" + c;
                                break;
                            case (2):
                                sounds.Add(temp);
                                //System.Console.WriteLine("Added: [{0}]", temp);
                                has_vowel = false;
                                temp = "" + c;
                                break;
                            case (3):
                                break;
                            case (4):
                                temp = temp + c;
                                break;
                            case (0):
                                temp = "" + c;
                                break;
                        }
                        last_was = 4;
                        break;
                    case (0):
                        if (temp.Length > 0)
                        {
                            sounds.Add(temp);
                            //System.Console.WriteLine("Added: [{0}]", temp);
                            has_vowel = false;
                        }
                        if (specials.Contains(c))
                        {
                            sounds.Add("_e_");
                            //System.Console.WriteLine("Added _e_");
                        }
                        temp = "";
                        last_was = 0;
                        break;
                }
                //System.Console.WriteLine("({0}, {1}) temp = {2}", last_was, c, temp);
            }

            return sounds;
        }

        public static bool vowel_check(string s)
        {
            string vowels = "weyuioa";
            bool ans = false;
            foreach (char c in s)
            {
                if (vowels.Contains(c))
                {
                    ans = true;
                    break;
                }
            }
            return ans;
        }

        public static List<string> solidificator(List<string> sounds)//Concatenates loose sounds without vowels into adjacent sides
        {
            int i = 0;
            while (i < sounds.Count)
            {
                if (!vowel_check(sounds[i]))
                {
                    if ((i > 0) & (sounds[i - 1] != "_e_"))
                    {
                        sounds[i - 1] = sounds[i - 1] + sounds[i];
                        sounds.RemoveAt(i);
                        i--;
                    }
                    else
                    {
                        if (((i + 1) < sounds.Count) & (sounds[i + 1] != "_e_"))
                        {
                            sounds[i + 1] = sounds[i] + sounds[i + 1];
                            sounds.RemoveAt(i);
                        }
                    }
                }
                i++;
            }
            return sounds;
        }

        public static bool separable_check(string s)
        {
            string vowels = "weyuioa";
            bool ans = false;
            bool v1 = false;
            bool c1 = false;
            bool v2 = false;

            if (s.Length > 2)
            {
                foreach (char c in s)
                {
                    if (vowels.Contains(c))
                    {
                        if (!v1)
                        {
                            v1 = true;
                        }
                        else
                        {
                            if (c1)
                            {
                                v2 = true;
                            }
                        }
                    }
                    else
                    {
                        if (v1 & (c != 'h'))
                        {
                            c1 = true;
                        }
                    }

                    if (v1 & c1 & v2)
                    {
                        ans = true;
                        return ans;
                    }
                }
            }

            return ans;
        }

        public static List<string> separator(List<string> sounds)//Concatenates loose sounds without vowels into adjacent sides
        {
            string vowels = "weyuioa";
            int pos;
            int i = 0;
            bool vowel_found;

            while (i < sounds.Count)
            {
                bool done = false;
                while (!done)
                {
                    done = true;
                    if (separable_check(sounds[i]))
                    {
                        done = false;
                        pos = sounds[i].Length - 1;//
                        vowel_found = false;
                        foreach (char c in sounds[i].Reverse())
                        {
                            if (vowels.Contains(c))
                            {
                                pos--;
                                vowel_found = true;
                            }
                            else
                            {
                                if (vowel_found)
                                {
                                    sounds.Insert(i + 1, sounds[i].Substring(pos));
                                    sounds[i] = sounds[i].Substring(0, pos);
                                    break;
                                }
                                else
                                {
                                    pos--;
                                }
                            }
                        }
                    }
                }

                i++;
            }

            return sounds;
        }

        public static List<string> specialificator(List<string> sounds)//Replaces special symbols
        {
            var new_sounds = sounds.Select(s => s.Replace("_e_", " ")).ToList();
            return new_sounds;
        }

        public static List<string> S5(string text)
        {
            List<string> sounds = new List<string>();

            text = normalize(text);
            text = simplificator(text);
            sounds = soundificator(text);
            sounds = solidificator(sounds);
            sounds = separator(sounds);
            sounds = specialificator(sounds);

            return sounds;
        }

    }
}
