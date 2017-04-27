using System;
using System.Collections.Generic;
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
    /// Interaction logic for ChatbotListView.xaml
    /// </summary>
    public partial class ChatbotListView : UserControl
    {
        List<Couppy> chatbots;

        public ChatbotListView()
        {
            InitializeComponent();
            chatbots = new List<Couppy>();

            Update();
        }

        public void Update()
        {
            chatbots.Clear(); ;
            chatBotStackPanel.Children.Clear();

            //Load all files in current directory with specified termination
            string[] chatbotFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.cp");

            //Go over all chatbotFiles
            for(int i=0;i< chatbotFiles.Length;i++)
            {
                //Load each bot
                Couppy loadedChatbot = Couppy.LoadFromFile(chatbotFiles[i]);

                //add bot to list
                chatbots.Add(loadedChatbot);

                //Create some visual
                Button selector = new Button();
                selector.DataContext = loadedChatbot;
                selector.Click += Selector_Click;
                selector.Content = loadedChatbot.getDescription();
                selector.Margin = new Thickness(6);
                chatBotStackPanel.Children.Add(selector);
            }
        }

        private void Selector_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.PushControl(new ChatbotView((sender as Button).DataContext as Couppy));
        }

        private void CreateChatbot_Click(object sender, RoutedEventArgs e)
        {
            CreateChatbotWindow createWindow = new CreateChatbotWindow(this);
            createWindow.ShowDialog();
        }
    }
}
