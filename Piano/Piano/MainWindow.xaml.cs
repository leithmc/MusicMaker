using Manufaktura.Controls.Model;
using Manufaktura.Music.Model;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Text.RegularExpressions;
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
using Microsoft.Win32;

namespace Piano
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // private String KeyName;
        private String MusicTitle;
        private int beatsPerMeasure;
        private int beatLength;
        private String keySignature;
        private ScoreVM model;
        private Boolean looped = false;
        private RhythmicDuration noteLength = RhythmicDuration.Quarter;
        private bool dotted = false;
        private string keyboard_Input = "None";
        private bool Loaded = false;
        // private Boolean FreshStart = true;

        private string[] keySigs = { "F", "C", "G", "D", "A", "E", "B", "F#", "C#", "G#", "D#", "A#", "E#", "Bb", "Eb", "Ab", "Db", "Gb" };
        string[] validBeatsPerMeasure = { "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16" };
        string[] validBeatLengths = { "2", "4", "8", "16" };

        // Dictionary to quickly map string inputs to RhythmicDuration objects. NoteLengths["HalfNote"] returns RhythmicDuration.Half.
        Dictionary<string, RhythmicDuration> NoteLengths = new Dictionary<string, RhythmicDuration>()
        {
            {"WholeNote", RhythmicDuration.Whole }, { "HalfNote", RhythmicDuration.Half }, {"QuarterNote", RhythmicDuration.Quarter },
            {"EigthNote", RhythmicDuration.Eighth }, {"SixteenthNote", RhythmicDuration.Sixteenth }, {"ThirtySecondNote", RhythmicDuration.D32nd }
        };




        /// <summary>
        /// Constructor for main xaml window.
        /// </summary>
        public MainWindow()
        {
            // Initialize the base window
            InitializeComponent();

            // Initialize the view model
            model = new ScoreVM();
            DataContext = model;
            model.loadStartData();

            //looks more professional starting with a new slate.
            OpenScoreCreationWindow();

            // Set viewer properties
            Viewer.RenderingMode = Manufaktura.Controls.Rendering.ScoreRenderingModes.SinglePage;

        }



        #region FileIO_EventHandlers
        /*** The code in this region handles the New, Load, Save, and Print buttons ***/


        /// <summary>
        /// Called when the New button is clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NewButton_Click(object sender, RoutedEventArgs e)
        {
            Loaded = false;
            OpenScoreCreationWindow();  // separate the event handler from the function so the function can be called from elsewhere
        }


        /// <summary>
        /// Opens the score creation window, populates combo boxes, and sets to default values.
        /// </summary>
        private void OpenScoreCreationWindow()
        {
            if (Loaded == false)
            {
                //hide parts of piano
                Print.Visibility = Visibility.Hidden;
                MusicSheet.Visibility = Visibility.Hidden;
                Notes_Rest.Visibility = Visibility.Hidden;
                Keyboard_Controls.Visibility = Visibility.Hidden;
                Piano_Black_Keys.Visibility = Visibility.Hidden;
                Piano_KeyBoard_layout.Visibility = Visibility.Hidden;
                Piano_White_Keys.Visibility = Visibility.Hidden;

                //cover keyboard like real piano
                KeyCover.Visibility = Visibility.Visible;

                // Populate the combo boxes
                BeatsMeasureCombo.ItemsSource = validBeatsPerMeasure;
                BeatLengthCombo.ItemsSource = validBeatLengths;
                KeySignatureCombo.ItemsSource = keySigs;

                // Reset selections to default values of 4/4 time, key of C, no title
                BeatsMeasureCombo.SelectedIndex = 2;
                BeatLengthCombo.SelectedIndex = 1;
                KeySignatureCombo.SelectedIndex = 1;
                TitleBox.Text = "";
                MusicNameLabel.Content = "";

                // Open the popup
                ScoreCreationWindow.Visibility = Visibility.Visible;
            }
            else
            {
                return;
            }
        }




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
            Loaded = false;
            KeyCover.Visibility = Visibility.Hidden;
            ScoreCreationWindow.Visibility = Visibility.Hidden;

            //make keys, music sheet, notes and keyboard control visible
            Print.Visibility = Visibility.Visible;
            Piano_KeyBoard_layout.Visibility = Visibility.Visible;
            Piano_White_Keys.Visibility = Visibility.Visible;
            Piano_Black_Keys.Visibility = Visibility.Visible;            
            MusicSheet.Visibility = Visibility.Visible;
            Notes_Rest.Visibility = Visibility.Visible;
            Keyboard_Controls.Visibility = Visibility.Visible;

            // Calculate key signature based on selected value from array
            // If selected index < 13, keyIndex - seleted index -1, else keyIndex = selected index
            Console.WriteLine(MusicTitle);
            MusicNameLabel.Content = MusicTitle;

            // Calculate key signature
            int keyIndex = (KeySignatureCombo.SelectedIndex < 13) ? KeySignatureCombo.SelectedIndex - 1 : 12 - KeySignatureCombo.SelectedIndex;
            Manufaktura.Controls.Model.Key key = new Manufaktura.Controls.Model.Key(keyIndex);

            // Calculate time signature
            TimeSignature timeSig = new TimeSignature(TimeSignatureType.Numbers, beatsPerMeasure, beatLength);


            // Build grand staff for now -- later may add options for more or fewer staves
            Staff treble = new Staff();
            MusicalSymbol[] elements = { Clef.Treble, key, timeSig }; // Add treble clef, key sig, time sig

            for (int i = 0; i < 3; i++)
            {
                treble.Elements.Add(elements[i]);
            }


            Staff bass = new Staff();
            elements[0] = Clef.Bass;        // Add bass clef, key sig, time sig

            for (int i = 0; i < 3; i++)
            {
                bass.Elements.Add(elements[i]);
            }

            Staff[] staves = { treble, bass };
            // Pass the title and the treble and bass staves to createNew
            model.createNew(TitleBox.Text, staves);

        }




        /// <summary>
        /// Called when the user clicks the 'Load' button.
        /// Opens the selected MusicXML file, converts it to a Score object, and loads it in the viewer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            MusicNameLabel.Content = "";

            // Close the score creation window if open   
            ScoreCreationWindow.Visibility = Visibility.Hidden;

            string fileName;
            // Use an OpenFile dilaog to get the file name
            Nullable<bool> result;
            OpenFileDialog dialog = new OpenFileDialog();
            try
            {
                result = dialog.ShowDialog();
            }

            catch (Exception ex)
            {
                MessageBox.Show("Could not open file dialog. " + ex.Message);
                OpenScoreCreationWindow();
                return;
            }

            if (result == true)
            {
                fileName = dialog.FileName;
            }

            else
            {
                MessageBox.Show("File could not be opened.");
                OpenScoreCreationWindow();
                return;
            }

            // Load the file
            try
            {
                model.loadFile(fileName);
                Loaded = true;


                //hide key cover
                KeyCover.Visibility = Visibility.Hidden;

                //make music sheet, keys, notes, keyboard settings visible
                Print.Visibility = Visibility.Visible;
                MusicSheet.Visibility = Visibility.Visible;
                Notes_Rest.Visibility = Visibility.Visible;
                Keyboard_Controls.Visibility = Visibility.Visible;                
                Piano_KeyBoard_layout.Visibility = Visibility.Visible;
                Piano_White_Keys.Visibility = Visibility.Visible;
                Piano_Black_Keys.Visibility = Visibility.Visible;
            }

            catch (Exception ex)
            {
                MessageBox.Show("Could not parse file: " + ex.Message);
                OpenScoreCreationWindow();
            }
        }





        /// <summary>
        /// Saves the current score as a MusicXml file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            model.save();
        }




        /// <summary>
        /// Formats the current score into one or more 8 1/2 x 11 pages and then sends them to the selected printer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            // To be implemented
            // One way to do this would be to generate a xaml popup like the one used for creating a new score,
            // but sized like a piece of paper, put a NoteViewer object in at the right size to match typical page margins,
            // load the score into the new noteViewer, and then print it from the xaml. http://www.c-sharpcorner.com/uploadfile/mahesh/printing-in-wpf/

            

            PrintDialog dialog = new PrintDialog();

            if (dialog.ShowDialog() == true)
            {
                Canvas music = new Canvas();
                music.Visibility = Visibility.Hidden;
                music = MusicSheet;
                music.Height = 300;
                music.Width = 670;                
                music.Margin = new Thickness(-150, 50, 50, 75);

                Play.Visibility = Visibility.Hidden;
                Stop.Visibility = Visibility.Hidden;
                Loop.Visibility = Visibility.Hidden;
                Reset.Visibility = Visibility.Hidden;

                dialog.PrintVisual(music, "Print Music Sheet");

                music.Visibility = Visibility.Hidden;
            }

            Play.Visibility = Visibility.Visible;
            Stop.Visibility = Visibility.Visible;
            Loop.Visibility = Visibility.Visible;
            Reset.Visibility = Visibility.Visible;
            MusicSheet.Margin = new Thickness(0, 0, 0, 0);
            MusicSheet.Visibility = Visibility.Visible;
        }

       



        #endregion

        #region PianoKeyHandlers
        /***** The code in this region handles everything to do with the piano keys. ****/





        /// <summary>
        /// Called when the mouse passes over a piano key.
        /// Turns the key aqua.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void keyOver(object sender, MouseEventArgs e) { ((Rectangle)sender).Fill = Brushes.Aqua; }





        /// <summary>
        /// Called when the mouse leaves a white key.
        /// Turns the key white.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void whiteKeyLeave(object sender, MouseEventArgs e) { ((Rectangle)sender).Fill = Brushes.White; }

        


        /// <summary>
        /// Called when the mouse leaves a black key.
        /// Turns the key black.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void blackKeyLeave(object sender, MouseEventArgs e) { ((Rectangle)sender).Fill = Brushes.Black; }

        


        /// <summary>
        /// Called by the mouse_down event of the piano keys.
        /// Turns the key yellow, adds its note to the score, and plays the note.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pianoKeyDown(object sender, MouseButtonEventArgs e)
        {
            ((Rectangle)sender).Fill = Brushes.Yellow;
            string keyName = (((FrameworkElement)e.Source).Name);
            Console.WriteLine(keyName);
            Note note = parseNoteFromInput(keyName);

            // Add note to staff
            addNoteToStaff(note);
        }


        /// <summary>
        /// Parses the name of the piano key to determine pitch and creates a new Noe object with
        /// the specified pitch and current note value.
        /// </summary>
        /// <param name="keyName">The name of the piano key from which to derive the pitch</param>
        /// <returns></returns>
        private Note parseNoteFromInput(string keyName)
        {
            // Start with a Pich object
            Pitch p;
            if (keyName.Length == 2) // If it's a white key...
                                     // Note name is the first letter, octave number is the second letter. Pass both to the Pitch constructor
            {
                p = new Pitch(keyName.Substring(0, 1), 0, int.Parse(keyName.Substring(1, 1)));
            }

            else
            {   // black key...
                // Determine if the current key uses sharps or flats (may be able to simplify this)
                Manufaktura.Controls.Model.Key key = null;
                foreach (var item in model.Data.FirstStaff.Elements)
                {
                    if (item.GetType() == typeof(Manufaktura.Controls.Model.Key))
                    {
                        key = (Manufaktura.Controls.Model.Key)item;
                        break;
                    }
                }

                string letter;
                int mod;    // sharp or flat
                if (key.Fifths > 0) // if sharp key, use the first letter and add a sharp
                {
                    letter = keyName.Substring(1, 1);
                    mod = 1;
                }
                else
                {       // flat key -- use the second letter and add a flat
                    letter = keyName.Substring(0, 1);
                    mod = -1;
                }
                p = new Pitch(letter, mod, int.Parse(keyName.Substring(2, 1))); // Third argument is octave
            }
            return new Note(p, noteLength);
        }



        /// <summary>
        /// Adds the note to the staff in the correct location
        /// </summary>
        /// <param name="note">The note to add</param>
        private void addNoteToStaff(Note note)
        {
            // Get the currently selected note, rest, or staff fragment
            var elem = Viewer.SelectedElement;
            if (elem == null)
            {
                MessageBox.Show("You must select a staff to write to or a note to insert after.");
                return;
            }

            //If user selects the staff itself, append to last note. Else insert after selected note.
            int staffIndex = model.Data.Staves.IndexOf(elem.Staff); // Get current staff
            if (elem.GetType() == typeof(StaffFragment)) model.Data.Staves[staffIndex].Elements.Add(note);  // Staff fragment, so add note to end of staff
            else
            {   // Not staff fragment, so insert note after selected element
                int index = elem.Staff.Elements.IndexOf(elem);
                model.Data.Staves[staffIndex].Elements.Insert(index + 1, note);
            }

            // Recusively fix overflowing measures, starting from the current one
            model.fitMeasure(note.Measure, model.TimeSig);

            // Trigger an update in the viewmodel
            model.updateView();

            // TODO: handle when clefs and signatures are selected
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
            //KeyName = "";
            //Console.WriteLine(KeyName);
        }
        #endregion




        #region NoteControls
        /**** The code in this region handles events from duration, key signature, time signature, and 
              any other controls that change the score except for the piano keys. ****/



        /// <summary>
        /// Note length Combo Box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NoteLengthSelection(object sender, MouseButtonEventArgs e)
        {
            string NoteName = (((FrameworkElement)e.Source).Name);
            ((Rectangle)sender).Stroke = Brushes.Yellow;

            if (NoteName == "Dot")
            {
                dotted = true;
            }
            else
            {
                noteLength = NoteLengths[NoteName];
            }
        }

        private void NoteMouseLeave(object sender, MouseEventArgs e)
        {
            
           ((Rectangle)sender).Stroke = Brushes.White;
            
        }


        private void NoteMouseEnter(object sender, MouseEventArgs e)
        {
            
           ((Rectangle)sender).Stroke = Brushes.Aqua;
            
        }


        //rest selection
        private void NoteRestSelection(object sender, MouseButtonEventArgs e)
        {
            string NoteRestName = (((FrameworkElement)e.Source).Name);
            ((Rectangle)sender).Stroke = Brushes.Yellow;

        }



        /// <summary>
        /// Radio Buttons... tested Default is none
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void KeyboardMappingSelection_Changed(object sender, RoutedEventArgs e)
        {

            if (rbNone.IsChecked == true)
            {
                keyboard_Input = rbNone.Name.ToString();
            }

            if (rbNumber.IsChecked == true)
            {
                keyboard_Input = rbNumber.Name.ToString();
            }

            if (rbLetter.IsChecked == true)
            {
                keyboard_Input = rbLetter.Name.ToString();
            }

            Console.WriteLine(keyboard_Input);
        }
        #endregion




        #region ScoreSetup
        /**** Code in this region handles events for controls involved in creating a new score. ****/


        /// <summary>
        /// Music Sheet Name
        /// Purged Input to only allow letters, Numbers and some specials
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MusicName(object sender, EventArgs e)
        {
            string TitlePurged = "";
            TextBox textbox = sender as TextBox;
            if (textbox != null)
            {
                String TCount = textbox.Text;

                Match match = Regex.Match(TCount, @"([A-Za-z0-9(){} ])+");

                if (match.Success)
                {
                    TitlePurged = TitlePurged + match.Value;
                }
            }

            // Console.WriteLine(TitlePurged);
            MusicTitle = TitlePurged;
        }






        /// <summary>
        /// Called when the user makes a selection from the Beats / Measure combobox.
        /// Stores the selected value and converts to int.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BeatsMeasureComboSelection_Changed(object sender, SelectionChangedEventArgs e)
        {
            string value = (string)BeatsMeasureCombo.SelectedValue;
            beatsPerMeasure = int.Parse(value);
        }






        /// <summary>
        /// Called when the user makes a selection from the Beat Length combobox.
        /// Stores the selected value and converts to int.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //Combo Box Beat Length
        private void BeatLengthComboSelection_Changed(object sender, SelectionChangedEventArgs e)
        {
            string value = (string)BeatLengthCombo.SelectedValue;
            beatLength = int.Parse(value);
        }






        /// <summary>
        /// Called when the user makes a selection from the Key Signature combobox.
        /// Stores the selected value.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void KeySignatureComboSelection_Changed(object sender, SelectionChangedEventArgs e)
        {
            keySignature = (string)KeySignatureCombo.SelectedValue;
        }






        /// <summary>
        /// Deletes the current score and replaces it with a blank default grand staff.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            // To be implemented
            TBI("Score reset");
        }
        #endregion




        #region Playback
        /**** Code in this region pertains to plaback controls. ****/


        /// <summary>
        /// Converts teh score to MIDI and plays the MIDI.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            // To be implemented
            TBI("Playback");
        }




        /// <summary>
        /// Stops playback.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            // To be implemented
            TBI("Playback");
        }




        /// <summary>
        /// loop button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoopButton_Click(object sender, RoutedEventArgs e)
        {
            //unloop method
            if (looped == true)
            {
                ((Button)sender).Background = Brushes.White;
                looped = false;
            }

            //looped method
            else
            {
                ((Button)sender).Background = Brushes.Green;
                looped = true;
            }
        }
        #endregion





        #region HelperFunctions
        // Just a temporary helper method to show when something isn't implemented yet.
        private void TBI(string feature)
        {
            MessageBox.Show(feature + " is not yet implemented.");
        }
        #endregion
    }
}
