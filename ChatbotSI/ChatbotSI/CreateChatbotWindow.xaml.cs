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
using System.Windows.Shapes;

namespace ChatbotSI
{
    /// <summary>
    /// Interaction logic for CreateChatbotWindow.xaml
    /// </summary>
    public partial class CreateChatbotWindow : Window
    {
        ChatbotListView caller;

        public CreateChatbotWindow(ChatbotListView caller)
        {
            InitializeComponent();
            this.caller = caller;


        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            string name = nameTextbox.Text;
            int layers = (int)layerSlider.Value;

            Couppy newChatbot = new Couppy(layers);
            newChatbot.name = name;
            newChatbot.SaveToFile(name + ".cp");
            caller.Update();
            Close();
        }
    }
}
