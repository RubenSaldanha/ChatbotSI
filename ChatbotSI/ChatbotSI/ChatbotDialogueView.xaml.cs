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
    /// Interaction logic for ChatbotDialogueView.xaml
    /// </summary>
    public partial class ChatbotDialogueView : UserControl
    {
        Couppy bot;

        Corpus.Dialogue dialogue;

        public ChatbotDialogueView(Couppy bot)
        {
            InitializeComponent();
            this.bot = bot;
            dialogue = new Corpus.Dialogue();
            inputTextBox.TextChanged += InputTextBox_TextChanged;
        }

        private void processInput(string text)
        {
            if (text[text.Length - 1] == '\n')
                text = text.Substring(0, text.Length - 2);

            //Add input to dialogue
            string[] newEntrances;
            if (dialogue.entrances == null)
            {
                dialogue.entrances = new string[1];
                dialogue.entrances[0] = text;
            }
            else
            {
                newEntrances = new string[dialogue.entrances.Length + 1];
                dialogue.entrances.CopyTo(newEntrances, 0);
                newEntrances[newEntrances.Length - 1] = text;
                dialogue.entrances = newEntrances;
            }

            //Compute response
            SymbolDialogue machineDialogue = Translator.textToSymbol(dialogue);
            string response = Translator.symbolToText(bot.response(machineDialogue));

            //Add response to dialogue
            newEntrances = new string[dialogue.entrances.Length + 1];
            dialogue.entrances.CopyTo(newEntrances, 0);
            newEntrances[newEntrances.Length - 1] = response;
            dialogue.entrances = newEntrances;

            //Update visuals
            UpdateDialogueBox();
        }

        private void UpdateDialogueBox()
        {
            dialoguePanel.Children.Clear();

            Label entrance;
            for(int i=0;i<dialogue.entrances.Length;i++)
            {
                entrance = new Label();
                entrance.Content = dialogue.entrances[i];
                dialoguePanel.Children.Add(entrance);
            }
        }

        private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox inputBox = sender as TextBox;

            string currentText = inputTextBox.Text;
            if (currentText != "")
            {
                if (currentText[currentText.Length - 1] == '\n')
                {
                    inputTextBox.Text = "";
                    processInput(currentText);
                }
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.PopControl();
        }
    }
}
