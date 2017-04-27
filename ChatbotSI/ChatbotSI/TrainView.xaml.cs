using System;
using System.Collections.Generic;
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

namespace ChatbotSI
{
    /// <summary>
    /// Interaction logic for TrainView.xaml
    /// </summary>
    public partial class TrainView : UserControl
    {
        bool training = false;

        Couppy chatbot;
        int selectedIndex;

        List<Button> corpusSelectors;

        Thread trainingThread;

        public TrainView(Couppy chatbot)
        {
            InitializeComponent();
            this.chatbot = chatbot;
            corpusSelectors = new List<Button>();
            selectedIndex = -1;

            for(int i=0;i<MainWindow.corpus.Count;i++)
            {
                Button selector = new Button();
                selector.Background = new SolidColorBrush(Colors.Gray);
                selector.Content = MainWindow.corpus[i].name;
                selector.DataContext = i;
                selector.Click += Selector_Click;

                corpusSelectors.Add(selector);
                corpusPanel.Children.Add(selector);
            }
        }

        private void Selector_Click(object sender, RoutedEventArgs e)
        {
            int index = (int)((sender as Button).DataContext);

            SelectCorpus(index);
        }
        private void SelectCorpus(int index)
        {
            if(selectedIndex != -1)
            {
                Button unselect = corpusSelectors[selectedIndex];
                unselect.Background = new SolidColorBrush(Colors.Gray);
            }

            selectedIndex = index;
            corpusSelectors[selectedIndex].Background = new SolidColorBrush(Colors.LightCyan);
        }

        delegate void callback();
        double trainMinutes;
        private void StartTrain()
        {
            chatbot.train(Translator.textToSymbol(MainWindow.corpus[selectedIndex]), trainMinutes);
            callback end = () => TrainEnded();
            Dispatcher.BeginInvoke(end);
        }
        private void TrainEnded()
        {
            training = false;
            trainButton.Content = "START Training";
        }

        private void Train_Click(object sender, RoutedEventArgs e)
        {
            if(!training)
            {
                if (selectedIndex >= 0)
                {
                    //Start Training
                    training = true;
                    trainButton.Content = "STOP Training";
                    trainMinutes = timeSlider.Value;
                    trainingThread = new Thread(StartTrain);
                    trainingThread.Start();
                }
            }
            else
            {
                //Stop Training
                training = false;
                trainButton.Content = "START Training";
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.PopControl();
        }
    }
}
