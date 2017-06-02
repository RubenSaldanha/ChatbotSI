using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    /// Interaction logic for ChatbotView.xaml
    /// </summary>
    public partial class ChatbotView : UserControl
    {
        Couppy chatbot;

        public ChatbotView(Couppy chatbot)
        {
            InitializeComponent();
            this.chatbot = chatbot;

            UpdateVisuals();
            MainWindow.controlChanged += UpdateVisuals;
        }

        public void UpdateVisuals()
        {

            nameLabel.Content = chatbot.name;
            cbPredictionCount.Content = chatbot.outputLayer.predictionCount;
            cbAccuracyCount.Content = chatbot.outputLayer.accuracy;

            layersPanel.Children.Clear();
            for(int i=0;i<chatbot.layerSizes.Length;i++)
            {
                StackPanel column = new StackPanel();
                column.Orientation = Orientation.Vertical;

                Label layerHeader = new Label();
                layerHeader.Content = "Layer " + i;
                layerHeader.FontSize = 16;
                layerHeader.HorizontalAlignment = HorizontalAlignment.Center;
                column.Children.Add(layerHeader);

                Label stateEntropy = new Label();
                stateEntropy.Content = "Entropy: " + ((i == 0) ? chatbot.outputLayer.stateEntropy : chatbot.deepLayers[i - 1].stateEntropy);
                stateEntropy.HorizontalAlignment = HorizontalAlignment.Center;
                column.Children.Add(stateEntropy);

                Border bView = new Border();
                bView.BorderBrush = new SolidColorBrush(Colors.LightGray);
                bView.BorderThickness = new Thickness(2);
                ScrollViewer sTview = new ScrollViewer();
                sTview.Height = 260;
                sTview.Background = new SolidColorBrush(Colors.LightGray);
                StackPanel entropies = new StackPanel();
                entropies.Margin = new Thickness(2);
                entropies.Orientation = Orientation.Vertical;
                entropies.Background = new SolidColorBrush(Colors.Wheat);
                entropies.Width = 120;

                int states = (i==0) ? chatbot.outputLayer.stateSize : chatbot.deepLayers[i - 1].stateSize;
                for(int k=0;k<states;k++)
                {
                    stateEntropy = new Label();
                    string stc = ("" + k).PadLeft(4) + " : ";
                    if (i == 0)
                        stc += chatbot.outputLayer.stateMetrics[k];
                    else
                        stc += chatbot.deepLayers[i - 1].stateMetrics[k];
                    stateEntropy.Content = stc;
                    entropies.Children.Add(stateEntropy);
                }
                sTview.Content = entropies;
                bView.Child = sTview;
                column.Children.Add(bView);

                layersPanel.Children.Add(column);
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.PopControl();
        }

        private void Train_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.PushControl(new TrainView(chatbot));
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            chatbot.SaveToFile(chatbot.name + ".cp");
        }

        private void Chat_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.PushControl(new ChatbotDialogueView(chatbot));
        }
    }
}
