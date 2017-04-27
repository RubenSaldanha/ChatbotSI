using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ChatbotSI
{
    //TODO
    //Vários conjuntos de testes (conversas p.ex)
    //CUDA - Threading
    //Restrictive predictor
    //TODO: Reverse predictor - might not be needed
    //TODO: Cascade function on StatePredictor
    //TODO Response algorithm (half-immediate, vs , generative)

    [Serializable()]
    public class Couppy
    {
        public string name;

        public HybridStatePredictor[] inputPredictors;

        //public bool training;

        //TODO: ResponseNet

        public int LayerCount
        {
            get { return inputPredictors.Length; }
        }

        public Couppy()
        {

        }
        public Couppy(int layers)
        {
            int symbolSize = Translator.SymbolCount;
            int stateSize = 64;

            if (symbolSize > 256)
                throw new NotImplementedException("Invalid symbol size, must be between 1 and 256");

            if (stateSize > 256)
                throw new NotImplementedException("Invalid state size, must be between 1 and 256");

            inputPredictors = new HybridStatePredictor[layers];

            inputPredictors[0] = new HybridStatePredictor(symbolSize, stateSize);

            for (int i = 1; i < layers; i++)
                inputPredictors[i] = new HybridStatePredictor(inputPredictors[i-1].stateSize, inputPredictors[i - 1].stateSize);
        }

        public void train(SymbolCorpus trainSet, double minutes)
        {
            for (int i = 0; i < inputPredictors.Length; i++)
            {
                //Training sucessive layers
                inputPredictors[i].train(trainSet, minutes/LayerCount);

                Console.WriteLine("Training for layer " + i + " finished with: ");
                Console.WriteLine(inputPredictors[i].getDescription());

                trainSet = inputPredictors[i].process(trainSet).states;
            }
        }

        public byte[] response(SymbolDialogue dialogue)
        {
            return inputPredictors[0].response(dialogue);
        }

        public void printCascade(int topLayer)
        {
            Console.WriteLine("Cascade: ");

            for(int i=0;i<inputPredictors[topLayer].stateSize;i++)
            {
                byte[] controls = inputPredictors[topLayer].cascade((byte)i);
                for(int j=topLayer - 1;j >= 0;j--)
                {
                    controls = inputPredictors[j].project(controls);
                }

                string text = Translator.symbolToText(controls);
                Console.Write("" + i + " :");
                for (int j = 0; j < text.Length && j < 64; j++)
                {
                    if (text[j] == '_')
                        break;

                    Console.Write(text[j]);
                }
                Console.WriteLine();
            }
        }

        public void SaveToFile(string file)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Couppy));
            TextWriter writer = new StreamWriter(file);
            serializer.Serialize(writer, this);
            writer.Close();
        }

        public static Couppy LoadFromFile(string file)
        {
            //Initialize deserializer
            XmlSerializer serializer = new XmlSerializer(typeof(Couppy));
            FileStream ReadFileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
            Couppy loadedChatbot = (Couppy)serializer.Deserialize(ReadFileStream);
            ReadFileStream.Close();

            return loadedChatbot;
        }

        public string getDescription()
        {
            return name;
        }
    }
}
