using Manufaktura.Controls.Model;
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
using System.Xml;

namespace Piano
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private String KeyName;
        private String ButtonName;
        private String Title;
        private int beatsPerMeasure;
        private int beatLength;
        private String keySignature;
        private ScoreVM Model;


        public MainWindow()
        {
            InitializeComponent();
            // for back programming.
            BackCode backcode = new BackCode();
            Model = new ScoreVM();
            DataContext = Model;
            Model.loadStartData();

        }




        /// <summary>
        /// Called when the mouse passes over a piano key.
        /// Turns the key aqua.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void keyOver(object sender, MouseEventArgs e)
        {
            ((Rectangle)sender).Fill = Brushes.Aqua;
        }

        /// <summary>
        /// Called when the mouse leaves a white key.
        /// Turns the key white.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void whiteKeyLeave(object sender, MouseEventArgs e)
        {
            ((Rectangle)sender).Fill = Brushes.White;
        }

        /// <summary>
        /// Called when the mouse leaves a black key.
        /// Turns the key black.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void blackKeyLeave(object sender, MouseEventArgs e)
        {
            ((Rectangle)sender).Fill = Brushes.Black;
        }



        
        /// <summary>
        /// Called by the mouse_down event of the piano keys.
        /// Turns the key yellow, adds its note to the score, and plays the note.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pianoKeyDown(object sender, MouseButtonEventArgs e)
        {
            ((Rectangle)sender).Fill = Brushes.Yellow;
            KeyName = (((FrameworkElement)e.Source).Name);
            Console.WriteLine(KeyName);
        }


        /// <summary>
        /// Called by the mouse_up event of the piano keys.
        /// Turns the key aqua.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pianoKeyUp(object sender, MouseButtonEventArgs e)
        {
            ((Rectangle)sender).Fill = Brushes.Aqua;
            KeyName = "";
            Console.WriteLine(KeyName);
        }


        ////Button actions
        //private void ButtonDown(object sender, RoutedEventArgs e)
        //{
        //    ButtonName = (((FrameworkElement)e.Source).Name);
        //    Console.WriteLine(ButtonName); //test name capture
        //    //do something here on backside
        //}
        
        private string[] keySigs = { "F", "C", "G", "D", "A", "E", "B", "F#", "C#", "G#", "D#", "A#", "E#", "Bb", "Eb", "Ab", "Db", "Gb"  };
        string[] validBeatsPerMeasure = { "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16" };
        string[] validBeatLengths = { "2", "4", "8", "16" };
        /// <summary>
        /// Opens the Create New Score popup window and populates the combo boxes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PopUp(object sender, RoutedEventArgs e)
        {
            // Populate the combo boxes
            Beats_Measure.ItemsSource = validBeatsPerMeasure;
            Beat_Length.ItemsSource = validBeatLengths;
            KeySignature.ItemsSource = keySigs;

            // Open the popup
            NewPop.IsOpen = true;
        }



        //Music Sheet Name
        private void MusicName(object sender, EventArgs e)
        {
            TextBox textbox = sender as TextBox;
            if (textbox != null)
            {
                String TCount = textbox.Text;
                Title = TCount;
                Console.WriteLine(Title);// tested passed
                //do something back end with this top count
            }
        }


        //Close popup and load NoteViewer
        /// <summary>
        /// Collects title, time, and key signature information from the Create New Score popup
        /// and uses them to create a new Score object and load it in the viewer.
        /// Called when the 'Start' button is clicked on the popup.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void createNew(object sender, RoutedEventArgs e)
        {
            // Close the popup
            NewPop.IsOpen = false;

            // Calculate key signature
            int keyIndex = (KeySignature.SelectedIndex < 13) ? KeySignature.SelectedIndex - 1 : 12 - KeySignature.SelectedIndex;
            Manufaktura.Controls.Model.Key key = new Manufaktura.Controls.Model.Key(keyIndex);

            // Calculate time signature
            TimeSignature timeSig = new TimeSignature(TimeSignatureType.Numbers, beatsPerMeasure, beatLength);

            // Build grand staff for now -- later may add options for more or fewer staves
            Staff treble = new Staff();
            MusicalSymbol[] elements = { Clef.Treble, key, timeSig };
            for (int i = 0; i < 3; i++) treble.Elements.Add(elements[i]);
            Staff bass = new Staff();
            elements[0] = Clef.Bass;
            for (int i = 0; i < 3; i++) bass.Elements.Add(elements[i]);
            Staff[] staves = { treble, bass };
            Model.createNew(Name.Text, staves);
        }



        
        /// <summary>
        /// Called when the user makes a selection from the Beats / Measure combobox.
        /// Stores the selected value and converts to int.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void beatsMeasure_Changed(object sender, SelectionChangedEventArgs e)
        {
            string value = (string) Beats_Measure.SelectedValue;
            beatsPerMeasure = int.Parse(value);
        }

        /// <summary>
        /// Called when the user makes a selection from the Beat Length combobox.
        /// Stores the selected value and converts to int.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //Combo Box Beat Length
        private void beatLength_changed(object sender, SelectionChangedEventArgs e)
        {
            string value = (string) Beat_Length.SelectedValue;
            beatLength = int.Parse(value);
        }

        /// <summary>
        /// Called when the user makes a selection from the Key Signature combobox.
        /// Stores the selected value.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void keySignature_changed(object sender, SelectionChangedEventArgs e)
        {
            keySignature = (string) KeySignature.SelectedValue;
        }


        /// <summary>
        /// Called when the user clicks the 'Load' button.
        /// Opens the selected MusicXML file, converts it to a Score object, and loads it in the viewer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Load_Click(object sender, RoutedEventArgs e)
        {
            string fileName;
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            Nullable<bool> result = dialog.ShowDialog();
            if (result == true) fileName = dialog.FileName;
            else
            {
                MessageBox.Show("File could not be opened.");
                return;
            }
            try
            {
                Model.loadFile(fileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        /// <summary>
        /// Saves the current score as a MusicXml file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Model.save();
        }


        /// <summary>
        /// Formats the current score into one or more 8 1/2 x 11 pages and then sends them to the selected printer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Print_Click(object sender, RoutedEventArgs e)
        {
            // To be implemented
            // One way to do this would be to generate a xaml popup like the one used for creating a new score,
            // but sized like a piece of paper, put a NoteViewer object in at the right size to match typical page margins,
            // load the score into the new noteViewer, and then print it from the xaml. http://www.c-sharpcorner.com/uploadfile/mahesh/printing-in-wpf/
        }

        /// <summary>
        /// Converts teh score to MIDI and plays the MIDI.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Play_Click(object sender, RoutedEventArgs e)
        {

        }

        /// <summary>
        /// Stops playback.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Stop_Click(object sender, RoutedEventArgs e)
        {

        }

        /// <summary>
        /// Deletes the current score and replaces it with a blank default grand staff.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Reset_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}