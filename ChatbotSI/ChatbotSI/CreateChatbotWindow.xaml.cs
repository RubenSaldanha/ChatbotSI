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

        List<int> layerConfigs;

        public CreateChatbotWindow(ChatbotListView caller)
        {
            InitializeComponent();
            this.caller = caller;

            layerConfigs = new List<int>();
            for (int i = 0; i < layerSlider.Maximum; i++)
                layerConfigs.Add(2);

            UpdateVisuals();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            string name = nameTextbox.Text;
            int layers = (int)layerSlider.Value;

            int[] layerSizes = new int[layers];
            for (int i = 0; i < layerSizes.Length; i++)
                layerSizes[i] = layerConfigs[i];

            Couppy newChatbot = new Couppy(layerSizes);
            newChatbot.name = name;
            newChatbot.SaveToFile(name + ".cp");
            caller.Update();
            Close();
        }

        private void layerSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (layersPanel == null)
                return;

            UpdateVisuals();
        }

        private void StateSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider sldr = (sender as Slider);
            int layer = (int)sldr.DataContext;
            layerConfigs[layer] = (int)sldr.Value;
        }

        private void UpdateVisuals()
        {
            layersPanel.Children.Clear();

            StackPanel descriptor;
            Label descriptorLabel;
            Slider stateSlider;
            GroupBox descriptorGB;
            for (int i = 0; i < layerSlider.Value; i++)
            {
                descriptorGB = new GroupBox();
                //descriptorGB.Header = "Layer " + (i + 1);
                descriptorGB.Background = new SolidColorBrush(Colors.LightYellow);
                descriptorGB.Height = 40;

                descriptor = new StackPanel();
                descriptor.Orientation = Orientation.Horizontal;
                descriptor.Height = 26;
                descriptor.VerticalAlignment = VerticalAlignment.Center;

                descriptorLabel = new Label();
                descriptorLabel.Content = "Layer " + (i + 1);
                descriptorLabel.Width = 60;
                descriptor.Children.Add(descriptorLabel);

                descriptorLabel = new Label();
                descriptorLabel.Content = "State size:";
                descriptor.Children.Add(descriptorLabel);

                stateSlider = new Slider();
                stateSlider.Minimum = 2;
                stateSlider.Maximum = 256;
                stateSlider.Width = 512;
                stateSlider.TickFrequency = 1;
                stateSlider.IsSnapToTickEnabled = true;
                stateSlider.DataContext = i;
                stateSlider.Margin = new Thickness(0, 5, 0, 0);
                stateSlider.Value = layerConfigs[i];
                stateSlider.ValueChanged += StateSlider_ValueChanged;
                descriptor.Children.Add(stateSlider);

                //<Label Content="{Binding ElementName=layerSlider, Path=Value, UpdateSourceTrigger=PropertyChanged}" Width="20" VerticalAlignment="Center" />
                descriptorLabel = new Label();
                Binding labelBind = new Binding();
                labelBind.Source = stateSlider;
                labelBind.Path = new PropertyPath("Value");
                labelBind.Mode = BindingMode.TwoWay;
                labelBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                BindingOperations.SetBinding(descriptorLabel, Label.ContentProperty, labelBind);
                descriptor.Children.Add(descriptorLabel);

                descriptorGB.Content = descriptor;
                layersPanel.Children.Add(descriptorGB);
            }
        }
    }
}
