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

        List<int> layerStateConfigs;
        List<int> layerOutputConfigs;

        public CreateChatbotWindow(ChatbotListView caller)
        {
            InitializeComponent();
            this.caller = caller;

            layerStateConfigs = new List<int>();
            layerOutputConfigs = new List<int>();
            for (int i = 0; i < layerSlider.Maximum; i++)
            {
                layerStateConfigs.Add(2);
                layerOutputConfigs.Add(2);
            }

            List<string> availableTranslators = new List<string>();
            availableTranslators.Add("CharToSymbol");
            availableTranslators.Add("SylabToSymbol");
            translatorComboBox.ItemsSource = availableTranslators;
            translatorComboBox.SelectedIndex = 0;

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

            int[] layerStateSizes = new int[layers];
            int[] layerOutputSizes = new int[layers];
            for (int i = 0; i < layerStateSizes.Length; i++)
            {
                layerStateSizes[i] = layerStateConfigs[i];
                layerOutputSizes[i] = layerOutputConfigs[i];
            }

            Translator usedTranslator;
            switch(translatorComboBox.SelectedIndex)
            {
                case 0:
                    usedTranslator = new CharToSymbolTranslator();
                    break;
                case 1:
                    usedTranslator = new SyllableToSymbolTranslator();
                    break;
                default:
                    return;
            }

            layerOutputSizes[layers - 1] = usedTranslator.getSymbolCount();

            Couppy newChatbot = new Couppy(layerStateSizes, layerOutputSizes, usedTranslator);
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
            layerStateConfigs[layer] = (int)sldr.Value;
        }

        private void OutputSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider sldr = (sender as Slider);
            int layer = (int)sldr.DataContext;
            layerOutputConfigs[layer] = (int)sldr.Value;
        }


        private void UpdateVisuals()
        {
            layersPanel.Children.Clear();

            StackPanel verticalSP;
            StackPanel stateDescriptor;
            StackPanel outputDescriptor;
            Label descriptorLabel;
            Slider stateSlider;
            Slider outputSlider;
            GroupBox descriptorGB;
            for (int i = 0; i < layerSlider.Value; i++)
            {
                //Groupbox
                descriptorGB = new GroupBox();
                descriptorGB.Background = new SolidColorBrush(Colors.LightYellow);
                descriptorGB.Height = 100;

                //main panel
                verticalSP = new StackPanel();
                verticalSP.Orientation = Orientation.Vertical;

                //First label
                descriptorLabel = new Label();
                descriptorLabel.Content = "Layer " + (i + 1);
                descriptorLabel.Width = 100;
                verticalSP.Children.Add(descriptorLabel);

                //State descriptor
                stateDescriptor = new StackPanel();
                stateDescriptor.Orientation = Orientation.Horizontal;
                stateDescriptor.Height = 26;
                stateDescriptor.VerticalAlignment = VerticalAlignment.Center;

                descriptorLabel = new Label();
                descriptorLabel.Content = "State size:";
                descriptorLabel.Width = 100;
                stateDescriptor.Children.Add(descriptorLabel);

                stateSlider = new Slider();
                stateSlider.Minimum = 2;

                if (i == layerSlider.Value - 1)
                    stateSlider.Maximum = 4096; // ushort.MaxValue;
                else
                    stateSlider.Maximum = 256;

                stateSlider.Width = 512;
                stateSlider.TickFrequency = 1;
                stateSlider.IsSnapToTickEnabled = true;
                stateSlider.DataContext = i;
                stateSlider.Margin = new Thickness(0, 5, 0, 0);
                stateSlider.Value = layerStateConfigs[i];
                stateSlider.ValueChanged += StateSlider_ValueChanged;
                stateDescriptor.Children.Add(stateSlider);

                //Ilustrative label
                descriptorLabel = new Label();
                Binding labelBind = new Binding();
                labelBind.Source = stateSlider;
                labelBind.Path = new PropertyPath("Value");
                labelBind.Mode = BindingMode.TwoWay;
                labelBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                BindingOperations.SetBinding(descriptorLabel, Label.ContentProperty, labelBind);
                stateDescriptor.Children.Add(descriptorLabel);

                verticalSP.Children.Add(stateDescriptor);

                //Output descriptor
                if(i == layerSlider.Value - 1)
                {
                    //last layer - output locked
                    descriptorLabel = new Label();
                    descriptorLabel.Content = "Output Size Locked. ";
                    verticalSP.Children.Add(descriptorLabel);
                }
                else
                {
                    //other layers - output config
                    outputDescriptor = new StackPanel();
                    outputDescriptor.Orientation = Orientation.Horizontal;
                    outputDescriptor.Height = 26;
                    outputDescriptor.VerticalAlignment = VerticalAlignment.Center;

                    //label
                    descriptorLabel = new Label();
                    descriptorLabel.Content = "Output size:";
                    descriptorLabel.Width = 100;
                    outputDescriptor.Children.Add(descriptorLabel);

                    outputSlider = new Slider();
                    outputSlider.Minimum = 2;
                    outputSlider.Maximum = 256;
                    outputSlider.Width = 512;
                    outputSlider.TickFrequency = 1;
                    outputSlider.IsSnapToTickEnabled = true;
                    outputSlider.DataContext = i;
                    outputSlider.Margin = new Thickness(0, 5, 0, 0);
                    outputSlider.Value = layerOutputConfigs[i];
                    outputSlider.ValueChanged += OutputSlider_ValueChanged;
                    outputDescriptor.Children.Add(outputSlider);

                    //Ilustrative label
                    descriptorLabel = new Label();
                    labelBind = new Binding();
                    labelBind.Source = outputSlider;
                    labelBind.Path = new PropertyPath("Value");
                    labelBind.Mode = BindingMode.TwoWay;
                    labelBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                    BindingOperations.SetBinding(descriptorLabel, Label.ContentProperty, labelBind);
                    outputDescriptor.Children.Add(descriptorLabel);

                    verticalSP.Children.Add(outputDescriptor);
                }

                descriptorGB.Content = verticalSP;
                layersPanel.Children.Add(descriptorGB);
            }
        }
    }
}
