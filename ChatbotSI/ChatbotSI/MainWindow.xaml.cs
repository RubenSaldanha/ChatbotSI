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

        public delegate void ControlChangedHandler();
        public static event ControlChangedHandler controlChanged;

        public MainWindow()
        {
            instance = this;
            InitializeComponent();
            corpus = new List<Corpus>();
            controlStack = new List<UserControl>();

            LoadCorpus();

            PushControl(new ChatbotListView());
        }

        private static void LoadCorpus()
        {
            corpus.Clear(); ;

            //Load all files in current directory with specified termination
            string[] corpusFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.txt");
            //Go over all chatbotFiles
            for (int i = 0; i < corpusFiles.Length; i++)
            {
                //Load each corpus
                //TODO Robustness, txt's are frequent
                Corpus loadedCorpus = Corpus.loadCorpus(corpusFiles[i]);

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

            controlChanged?.Invoke();
        }
        public static void PushControl(UserControl control)
        {
            controlStack.Add(control);
            MainControl.Content = control;
            controlChanged?.Invoke();
        }

    }
}
