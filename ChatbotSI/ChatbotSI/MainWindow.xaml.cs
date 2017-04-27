using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Serialization;

namespace ChatbotSI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static MainWindow instance;

        public static List<Corpus> corpus;

        static UserControl MainControl
        {
            get { return instance.mainControl; }
            set { instance.mainControl = value; }
        }
        static List<UserControl> controlStack;

        public MainWindow()
        {
            instance = this;
            InitializeComponent();
            corpus = new List<Corpus>();
            controlStack = new List<UserControl>();
            Closed += MainWindow_Closed;

            LoadCorpus();

            PushControl(new ChatbotListView());
        }

        private static void LoadCorpus()
        {
            corpus.Clear(); ;

            //Load all files in current directory with specified termination
            string[] chatbotFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.txt");

            //Go over all chatbotFiles
            for (int i = 0; i < chatbotFiles.Length; i++)
            {
                //Load each bot
                //TODO Robustness, txt's are frequent
                Corpus loadedCorpus = Corpus.loadCorpus(chatbotFiles[i]);

                //add corpus to list
                corpus.Add(loadedCorpus);
            }
        }

        public static void PopControl()
        {
            controlStack.RemoveAt(controlStack.Count - 1);
            if (controlStack.Count > 0)
                MainControl.Content = controlStack[controlStack.Count - 1];
            else
                Application.Current.Shutdown();
        }
        public static void PushControl(UserControl control)
        {
            controlStack.Add(control);
            MainControl.Content = control;
        }
        void TrainCascade1()
        {
            Corpus text = Corpus.loadCorpus("Corp3.txt");
            SymbolCorpus trainSet = Translator.textToSymbol(text);

            Couppy ai = new Couppy(1);

            ai.train(trainSet, 180);

            //text = text.subCorpus(0.01, 0);

            SymbolCorpus test = Translator.textToSymbol(text);
            SymbolCorpus g0 = ai.inputPredictors[0].process(test).states;

            //printProcessResult(ai.inputPredictors[0], test);

            for (int i = 0; i < ai.LayerCount; i++)
            {
                Console.WriteLine("Printing CASCADE at layer: " + i);
                ai.printCascade(i);
            }

            int a = 2;
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {

        }

        HybridStatePredictor Load()
        {
            HybridStatePredictor pred = HybridStatePredictor.loadFromFile("pred.tp");

            Corpus cornwell = Corpus.getCornwell();
            cornwell = cornwell.subCorpus(0.00001, 1);

            //Process(pred, cornwell);

            return pred;
        }
        void StartTrain()
        {
            Corpus text = Corpus.loadCorpus("Corp3.txt");
            //text = text.subCorpus(0.00001, 1);
            Console.WriteLine("Corpus Stats: \n" + text.getCorpusStats());

            Console.WriteLine("Used corpus: ");
            //Console.WriteLine(text.toString());

            HybridStatePredictor l0 = HybridStatePredictor.loadFromFile("l0.tp");
            HybridStatePredictor.CorpusResult g0 = l0.process(Translator.textToSymbol(text));

            HybridStatePredictor l1 = HybridStatePredictor.loadFromFile("l1.tp");
            HybridStatePredictor.CorpusResult g1 = l1.process(g0.states);

            SymbolCorpus trainSet = g0.states;

            Console.WriteLine("Translated corpus: ");
            //text = Translator.symbolToCorpus(trainSet);
            //Console.WriteLine(text.toString());

            //Predictor
            Console.WriteLine("----------- TRAINING ------\n");
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            HybridStatePredictor testPred = new HybridStatePredictor(l0.stateSize, 32);
            for (int i = 0; i < int.MaxValue; i++)
            {
                if (stopwatch.ElapsedMilliseconds > 1000 * 60 * 15)
                    break;
                testPred.train(trainSet, 10000);
                Console.WriteLine( i + " : " + testPred.getStats());
                testPred.saveToFile("cc" + i + ".tp");
            }
            stopwatch.Stop();
            long elapsedTime = stopwatch.ElapsedMilliseconds;
            Console.WriteLine("----------- TRAINING END -----\nRuntime at: \nMiliseconds: " + elapsedTime + "\nSeconds: " + elapsedTime / 1000f + "\nMinutes" + (elapsedTime / 1000f) / 60f);

            Console.WriteLine();
            Console.WriteLine("Train result: \n" +testPred.getDescription());
            Console.WriteLine();

            //Process(testPred, text);


            Console.WriteLine("Cascade: ");
            for (int i = 0; i < testPred.stateSize; i++)
            {
                Console.WriteLine("" + i + " :");
                Console.WriteLine(Translator.symbolToText(testPred.cascade((byte)i)));
            }

            testPred.saveToFile("predl1.tp");

            //Load();
        }

        void printProcessResult(HybridStatePredictor pred, SymbolCorpus original)
        {
            //Print original
            Console.WriteLine("Original: \n");
            Console.WriteLine(Translator.symbolToText(original).toString());

            HybridStatePredictor.CorpusResult result = pred.process(original);
            //Print hits
            Console.WriteLine("Hits: \n");
            SymbolCorpus hits = result.hits;
            Console.WriteLine(Translator.symbolToText(hits).toString());

            //Output controls
            SymbolCorpus states = result.states;
            SymbolCorpus projection = pred.project(states);
            //Print controlled
            Console.WriteLine("Reproduced: \n");
            Console.WriteLine(Translator.symbolToText(projection).toString());
        }
    }
}
