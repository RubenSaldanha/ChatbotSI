using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        int seconds;

        System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();

        public TrainView(Couppy chatbot)
        {
            InitializeComponent();
            this.chatbot = chatbot;
            corpusSelectors = new List<Button>();
            selectedIndex = -1;

            for(int i=0;i<MainWindow.corpus.Count;i++)
            {
                Button selector = new Button();
                selector.Background = new SolidColorBrush(Colors.LightYellow);
                GroupBox container = new GroupBox();
                container.Header = MainWindow.corpus[i].name;
                container.FontSize = 18;
                container.BorderThickness = new Thickness(0);
                container.HorizontalAlignment = HorizontalAlignment.Stretch;


                Label stats = new Label();
                stats.FontSize = 14;
                string sts = "";
                sts += "Dialogue count: " + ("" + MainWindow.corpus[i].getDialogueCount()).PadRight(20) + "   ";
                sts += "Sentence count: " + ("" + MainWindow.corpus[i].getSentenceCount()).PadRight(20) + "   ";
                sts += "Character count: " + ("" + MainWindow.corpus[i].getCharCount()).PadRight(20);
                stats.Content = sts;

                container.Content = stats;

                selector.Content = container;
                selector.DataContext = i;
                selector.Height = 64;
                selector.Width = 700;
                selector.FontSize = 18;
                selector.Click += Selector_Click;
                selector.Margin = new Thickness(2);
                selector.HorizontalContentAlignment = HorizontalAlignment.Stretch;

                corpusSelectors.Add(selector);
                corpusPanel.Children.Add(selector);
            }

            timer.Interval = new TimeSpan(0, 0, 1);
            timer.Tick += timer_tick;

            Couppy.TrainingUpdated += Couppy_TrainingUpdated;
        }

        string currentStatus;
        private void Couppy_TrainingUpdated(string message)
        {
            currentStatus = message;

            Dispatcher.Invoke(updateStatusLabel);
        }
        private void updateStatusLabel()
        {
            statusText.Text = currentStatus;
            //statusScroller.ScrollToEnd();
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
                unselect.Background = new SolidColorBrush(Colors.LightYellow);
                unselect.Foreground = new SolidColorBrush(Colors.Black);
                ((corpusSelectors[selectedIndex].Content as GroupBox).Content as Label).Foreground = new SolidColorBrush(Colors.Black);
            }

            selectedIndex = index;
            corpusSelectors[selectedIndex].Background = new SolidColorBrush(Colors.Salmon);
            corpusSelectors[selectedIndex].Foreground = new SolidColorBrush(Colors.White);
            ((corpusSelectors[selectedIndex].Content as GroupBox).Content as Label).Foreground = new SolidColorBrush(Colors.White);
        }

        private void Train_Click(object sender, RoutedEventArgs e)
        {
            if(!training)
            {
                if (selectedIndex >= 0)
                {
                    //Start Training
                    timer.Start();

                    training = true;
                    trainingTime.Background = new SolidColorBrush(Colors.Salmon);
                    trainingTime.Foreground = new SolidColorBrush(Colors.White);
                    trainButton.Content = "Stop Training";
                    chatbot.StartTrain(Translator.textToSymbol(MainWindow.corpus[selectedIndex]));
                }
            }
            else
            {
                timer.Stop();
                //Stop Training
                training = false;
                trainingTime.Background = new SolidColorBrush(Colors.Transparent);
                trainingTime.Foreground = new SolidColorBrush(Colors.Black);
                trainButton.Content = "Start Training";
                chatbot.StopTrain();
                //chatbot.printCascade();
            }
        }
        private void timer_tick(object sender, EventArgs e)
        {
            seconds++;

            TimeSpan time = TimeSpan.FromSeconds(seconds);
            trainingTime.Content = time.ToString(@"hh\:mm\:ss");
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.PopControl();
        }
    }
}
