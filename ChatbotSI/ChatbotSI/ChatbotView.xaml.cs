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

            mainLabel.Content = chatbot.getDescription();
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
