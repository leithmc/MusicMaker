using LData;
using Manufaktura.Controls.Model;
using Manufaktura.Music.Model;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Piano
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IDisposable
    {
        private string MusicTitle;
        private int beatsPerMeasure;
        private int beatLength;
        private ScoreVM model;
        private bool looped = false;
        private RhythmicDuration noteLength = RhythmicDuration.Quarter;
        private string keyboard_Input = "None";
        private bool isLoaded = false;
        private string Selected = "QuarterNote";
        private object SelectedNote;
        private object DottedSelected;
        private string HoldSelected = "";
        private RoutedCommand cmdDelete;
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

            SelectedNote = QuarterNote;

            // Initialize the view model
            model = new ScoreVM();
            DataContext = model;
            cmdDelete = new RoutedCommand();
            cmdDelete.InputGestures.Add(new KeyGesture(System.Windows.Input.Key.Delete));
            CommandBindings.Add(new CommandBinding(cmdDelete, delete));

            // Open the score creation window
            OpenScoreCreationWindow();
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
            isLoaded = false;
            OpenScoreCreationWindow();  // separate the event handler from the function so the function can be called from elsewhere
        }


        /// <summary>
        /// Opens the score creation window, populates combo boxes, and sets to default values.
        /// </summary>
        private void OpenScoreCreationWindow()
        {
            if (isLoaded == false)
            {
                //hide parts of piano
                Print.Visibility = Visibility.Hidden;
                MusicSheet.Visibility = Visibility.Hidden;
                Notes_Rest.Visibility = Visibility.Hidden;
                Keyboard_Controls.Visibility = Visibility.Hidden;
                Piano_Black_Keys.Visibility = Visibility.Hidden;
                Piano_KeyBoard_layout.Visibility = Visibility.Hidden;
                Piano_White_Keys.Visibility = Visibility.Hidden;
                WorkingButtons.Visibility = Visibility.Hidden;

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
            else return;
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
            isLoaded = false;
            KeyCover.Visibility = Visibility.Hidden;
            ScoreCreationWindow.Visibility = Visibility.Hidden;

            //make keys, music sheet, notes and keyboard control visible
            Print.Visibility = Visibility.Visible;
            Piano_KeyBoard_layout.Visibility = Visibility.Visible;
            Piano_White_Keys.Visibility = Visibility.Visible;
            Piano_Black_Keys.Visibility = Visibility.Visible;
            MusicSheet.Visibility = Visibility.Visible;
            Notes_Rest.Visibility = Visibility.Visible;
            Keyboard_Controls.Visibility = Visibility.Hidden;
            WorkingButtons.Visibility = Visibility.Visible;

            //reset note selection to quarter note
            NoteSelectionReset();
            
            // Calculate key signature based on selected value from array
            Console.WriteLine(MusicTitle);
            MusicNameLabel.Content = MusicTitle;

            // Calculate key signature
            int keyIndex = (KeySignatureCombo.SelectedIndex < 13) ? KeySignatureCombo.SelectedIndex - 1 : 12 - KeySignatureCombo.SelectedIndex;
            Manufaktura.Controls.Model.Key key = new Manufaktura.Controls.Model.Key(keyIndex);
            model.KeySig = new Manufaktura.Controls.Model.Key(keyIndex);

            // Calculate time signature
            TimeSignature timeSig = new TimeSignature(TimeSignatureType.Numbers, beatsPerMeasure, beatLength);
            model.TimeSig = new TimeSignature(TimeSignatureType.Numbers, beatsPerMeasure, beatLength);

            // Create a single staff score
            model.FileName = "";
            model.createNew(key, timeSig);
            Viewer.ScoreSource = model.Data;
            model.ResetPlayer();
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
                isLoaded = true;

                //reset note selection to quarter note
                NoteSelectionReset();

                //hide key cover
                KeyCover.Visibility = Visibility.Hidden;

                //make music sheet, keys, notes, keyboard settings visible
                Print.Visibility = Visibility.Visible;
                MusicSheet.Visibility = Visibility.Visible;
                Notes_Rest.Visibility = Visibility.Visible;
                Keyboard_Controls.Visibility = Visibility.Hidden;
                Piano_KeyBoard_layout.Visibility = Visibility.Visible;
                Piano_White_Keys.Visibility = Visibility.Visible;
                Piano_Black_Keys.Visibility = Visibility.Visible;
                WorkingButtons.Visibility = Visibility.Visible;
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
            PrintDialog dialog = new PrintDialog();

            if (dialog.ShowDialog() == true)
            {
                Canvas music = new Canvas();
                music.Visibility = Visibility.Hidden;
                music = MusicSheet;
                music.Height = 1200;
                music.Width = 900;

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
            MusicSheet.Margin = new Thickness(225, 75, 0, 0);
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
            addNoteOrRestToStaff(note);
        }

        /// <summary>
        /// Gets the LStaff object that corresponds to the Staff object that contatins the specified element.
        /// </summary>
        /// <param name="elem">The element contained.</param>
        /// <returns>The corresponding LStaff.</returns>
        private LStaff getLStaff(MusicalSymbol elem)
        {
            if (elem == null) return model.staves[0];
            int index = model.Data.Staves.IndexOf(elem.Staff);
            return model.staves[index];
        }

        /// <summary>
        /// Identifies whether the selected element can be safely followed by a note or rest.
        /// </summary>
        /// <param name="elem">The element to verify.</param>
        /// <returns>true if elem is a note or rest; false otherwise.</returns>
        private bool isValidTarget(MusicalSymbol elem)
        {
            if (elem == null) return false;
            return (elem.GetType().IsSubclassOf(typeof(NoteOrRest)));
        }


        /// <summary>
        /// Parses the name of the piano key to determine pitch and creates a new Noe object with
        /// the specified pitch and current note value.
        /// </summary>
        /// <param name="keyName">The name of the piano key from which to derive the pitch</param>
        /// <returns></returns>
        private Note parseNoteFromInput(string keyName)
        {
            // Start with a Pitch object
            Pitch p;
            if (keyName.Length == 2) // White key. Note name is the first letter, octave number is the second letter. Pass both to the Pitch constructor
                p = new Pitch(keyName.Substring(0, 1), 0, int.Parse(keyName.Substring(1, 1)));

            else // Black key. AB3 means A# or Bb in third octave.
            {
                string letter;
                int mod;    // sharp or flat
                if (model.KeySig.Fifths > 0) // if sharp key, use the first letter and add a sharp
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
        /// <param name="nr">The note to add</param>
        private void addNoteOrRestToStaff(NoteOrRest nr)
        {
            // New for LStaff
            var elem = Viewer.SelectedElement;
            LStaff staff = getLStaff(elem);
            NoteOrRest noteToPlay;
            if (isValidTarget(elem))
            {
                LMeasure measure = staff.getMeasure(elem);
                staff.AddAfter(elem, nr);
                if (measure.Contains(nr)) Viewer.SelectedElement = nr;
                else if (measure.Node.Next != null && measure.Node.Next.Value.Count > 0)
                    Viewer.SelectedElement = measure.Node.Next.Value.First.Value;
                noteToPlay = (NoteOrRest) Viewer.SelectedElement;
            }
            else
            {
                staff.Add(nr);
                noteToPlay = (NoteOrRest) staff.Last.Value.Last(e => e.GetType().IsSubclassOf(typeof(NoteOrRest)));
            }

            // Play the note
            if (noteToPlay.GetType() == typeof(Note))
            {
                model.PlayNote((Note)noteToPlay);
            }

            // Update the view model to make the Play command available
            model.updateView();
        }

        /// <summary>
        /// Deletes the selected note or rest.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void delete(object sender, ExecutedRoutedEventArgs e)
        {
            var elem = Viewer.SelectedElement;
            if (elem != null & elem.Staff.Elements.Contains(elem))
            {
                Viewer.SelectedElement = null;
                model.staves[model.Data.Staves.IndexOf(elem.Staff)].Remove(elem);
            }
            else MessageBox.Show("You must select a note or rest to delete. To delete all, click the Reset button.");
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
        }
        #endregion

        #region NoteControls
        /**** The code in this region handles events from duration, key signature, time signature, and 
              any other controls that change the score except for the piano keys. ****/


        /// <summary>
        /// Sets the default note value to the selected note type. Called when any
        /// of the note value buttons are clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NoteLengthSelection(object sender, MouseButtonEventArgs e)
        {
            string NoteName = (((FrameworkElement)e.Source).Name);

            if (DottedSelected != null)
            {
                if (DottedSelected != sender)
                {
                    ((Rectangle)DottedSelected).Stroke = Brushes.White;
                    DottedSelected = null;
                }
            }        

            if (NoteName == "Dot")
            {
                //hold previous selected note for color
                object HoldSelectedwithDot = SelectedNote;
                ((Rectangle)HoldSelectedwithDot).Stroke = Brushes.Yellow;
                HoldSelected = (((FrameworkElement)HoldSelectedwithDot).Name);

                //bring in dot and hold color
                DottedSelected = sender;
                noteLength.Dots++;
                ((Rectangle)DottedSelected).Stroke = Brushes.Yellow;
                Selected = (((FrameworkElement)e.Source).Name);
            }

            else
            {
                noteLength = NoteLengths[NoteName];
                noteLength.Dots = 0;

                //Change previos selected note outline to white
                if (SelectedNote != sender)
                {
                    ((Rectangle)SelectedNote).Stroke = Brushes.White;
                }
                
                SelectedNote = sender;
                Selected = (((FrameworkElement)e.Source).Name);
                ((Rectangle)sender).Stroke = Brushes.Yellow;
            }
        }


        /// <summary>
        /// Resets the selected note value to QuarterNote when a new score is created or loaded.
        /// </summary>
        private void NoteSelectionReset()
        {
            object PreviousSelected = SelectedNote;

            if (DottedSelected != null)
            {
                ((Rectangle)DottedSelected).Stroke = Brushes.White;
                DottedSelected = null;
                noteLength.Dots = 0;
            }

            SelectedNote = QuarterNote;
            HoldSelected = "";
            Selected = "QuarterNote";

            if(SelectedNote != PreviousSelected)
            {
                ((Rectangle)PreviousSelected).Stroke = Brushes.White;
                ((Rectangle)SelectedNote).Stroke = Brushes.Yellow;
                noteLength = RhythmicDuration.Quarter;
                Selected = "QuarterNote";
            }
            else return;
        }



        /// <summary>
        /// Adds the selected Rest to the score.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NoteRestSelection(object sender, MouseButtonEventArgs e)
        {
            string NoteRestName = (((FrameworkElement)e.Source).Name);
            RhythmicDuration previousNoteLength = noteLength;

            // Update note value to match the rest
            switch (NoteRestName)
            {
                case "WholeRest":
                    noteLength = RhythmicDuration.Whole;
                    break;
                case "HalfRest":
                    noteLength = RhythmicDuration.Half;
                    break;
                case "QuarterRest":
                    noteLength = RhythmicDuration.Quarter;
                    break;
                case "EighthRest":
                    noteLength = RhythmicDuration.Eighth;
                    break;
                case "SixteenthRest":
                    noteLength = RhythmicDuration.Sixteenth;
                    break;
                case "ThirtySecondRest":
                    noteLength = RhythmicDuration.D32nd;
                    break;
                default:
                    break;
            }
            noteLength.Dots = 0;

            // Add the rest to the score
            addNoteOrRestToStaff(new Rest(noteLength));
            
            // Restore to previous value
            noteLength = previousNoteLength;
        }


        /// <summary>
        /// Called when the cursor leaves the region of a piano key. Reverts the key color to
        /// the previous value unless selected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NoteMouseLeave(object sender, MouseEventArgs e)
        {
            String Over = (((FrameworkElement)e.Source).Name);
            if (Over == Selected || Over == HoldSelected) return;
            else ((Rectangle)sender).Stroke = Brushes.White;
        }

        /// <summary>
        /// Called when the cursor enters the region of a piano key. Changes the color of the key.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NoteMouseEnter(object sender, MouseEventArgs e)
        {
            String Over = (((FrameworkElement)e.Source).Name);
            if (Over == Selected || Over == HoldSelected) return;
            else ((Rectangle)sender).Stroke = Brushes.Aqua;            
        }        


        /// <summary>
        /// Controls the choice of keyboard mappings.
        /// Not implemented.
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
        /// Deletes the current score and replaces it with a blank default staff.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            model.Data = null;
            model.updateView();
            model.createNew(model.KeySig, model.TimeSig);
            Viewer.ScoreSource = model.Data;
            model.ResetPlayer();
        }
        #endregion

        #region Playback
        /**** Code in this region pertains to plaback controls. ****/

        /// <summary>
        /// Sets the looping property of the player when the Loop button is clicked.
        /// Currently inactive.
        /// </summary>
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

        /// <summary>
        /// Cleans up resources.
        /// </summary>
        /// <param name="cleanAll">true to dispose managed resources. false to dispose native only.</param>
        protected virtual void Dispose(bool cleanAll)
        {
            if (cleanAll)
            {
                model.Dispose();
            }
        }

        /// <summary>
        /// Cleans up resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

    }
}
