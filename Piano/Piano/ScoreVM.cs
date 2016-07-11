using LData;
using Manufaktura.Controls.Audio;
using Manufaktura.Controls.Desktop.Audio;
using Manufaktura.Controls.Model;
using Manufaktura.Controls.Parser;
using Manufaktura.Model.MVVM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml.Linq;

namespace Piano
{
    public class ScoreVM : ViewModel, IDisposable
    {
        private string fileName = "";   // Name of the file to load from or save to.
        private Score data;
        private ScorePlayer player;
        internal List<LStaff> staves;

        public ScorePlayer Player => player;
        public PlayCommand PlayCommand { get; }
        public StopCommand StopCommand { get; }

        /// <summary>
        /// Constructor. Initializes the view model in connection to its playback commands.
        /// </summary>
        public ScoreVM()
        {
            PlayCommand = new PlayCommand(this);
            StopCommand = new StopCommand(this);
        }

        /// <summary>
        /// Holds the current Score object. Refreshing the public property updates the viewer.
        /// </summary>
        public Score Data
        {
            get { return data; }
            set
            {
                data = value;
                OnPropertyChanged();            // OnPropertyChanged events sync the databound viewer and player
                OnPropertyChanged(() => Player);
                OnPropertyChanged(() => Data);
                PlayCommand?.FireCanExecuteChanged();
                StopCommand?.FireCanExecuteChanged();
            }
        }

        private Key keySig;
        /// <summary>
        /// Holds the current key signature
        /// </summary>
        public Key KeySig
        {
            get { return keySig; }
            set { keySig = value; }
        }

        private TimeSignature timeSig;

        /// <summary>
        /// Holds the current time signature
        /// </summary>
        public TimeSignature TimeSig
        {
            get { return timeSig; }
            set { timeSig = value; }
        }

        public bool isLooping { get; internal set; }

        public string FileName
        {
            get
            {
                return fileName;
            }

            set
            {
                fileName = value;
            }
        }

        /// <summary>
        /// Populates the note viewer with an empty single staff. Does not set fileName.
        /// </summary>
        public void loadStartData()
        {
            Data = createStartingStaff();   // This will switch to createGrandStaff once the note entry bugs are fixed
            ResetPlayer();
        }

        /// <summary>
        /// Generates an empty grand staff in the specified key and time signature.
        /// </summary>
        /// <param name="key">A Key enumeration value to specify the starting key of the piece</param>
        /// <param name="timeSig">A TimeSignature object representing the time signature of the piece</param>
        /// <returns>A score object containing a grand staff in the specified key and time signature</returns>
        public Score createStartingStaff()
        {
            // Create a score object with a single staff
            var score = Score.CreateOneStaffScore();

            // Add treble clef
            score.FirstStaff.Elements.Add(Clef.Treble);

            // add key signature. The 0 in the Key constructor means no sharps or flats. 
            // negative numbers are flat keys, positive for sharp keys.
            keySig = new Key(0);
            score.FirstStaff.Elements.Add(keySig);
            // Set time sig to 4/4
            timeSig = TimeSignature.CommonTime;
            score.FirstStaff.Elements.Add(timeSig);

            // Populate data model
            staves = new List<LStaff>();
            staves.Add(new LStaff(score.FirstStaff, Clef.Treble, keySig, timeSig));
            return score;
        }



        /// <summary>
        /// Generates an empty grand staff in the specified key and time signature.
        /// </summary>
        /// <param name="key">A Key enumeration value to specify the starting key of the piece</param>
        /// <param name="timeSig">A TimeSignature object representing the time signature of the piece</param>
        /// <returns>A score object containing a grand staff in the specified key and time signature</returns>
        public Score createGrandStaff(Key key, TimeSignature timeSig)
        {
            var score = Score.CreateOneStaffScore();    // See createStartingStaff for line by line comments
            score.FirstStaff.Elements.Add(Clef.Treble);
            this.keySig = key;
            score.FirstStaff.Elements.Add(keySig);
            this.timeSig = timeSig;
            score.FirstStaff.Elements.Add(timeSig);
            score.FirstStaff.Elements.Add(new Barline());
            Staff bass = new Staff();
            bass.Elements.Add(Clef.Bass);
            bass.Elements.Add(key);
            bass.Elements.Add(timeSig);
            // Add bass staff
            score.Staves.Add(bass);
            keySig = key;
            return score;
        }


        /// keep this for later
        /// <summary>
        /// Returns a default empty grand staff.
        /// </summary>
        /// <returns>A Score object containing an empty grand staff in C major and 4/4 time</returns>
        public Score createGrandStaff()
        {
            return createGrandStaff(new Key(0), TimeSignature.CommonTime);
        }



        /// <summary>
        /// Loads the viewer with a Score object generated from the specified MusicXml file.
        /// </summary>
        public void loadFile(string fileName)
        {
            this.fileName = fileName;
            if (File.Exists(fileName))
            {
                // Parser instance converts between Score object and MusicXML
                var parser = new MusicXmlParser();
                Score score = parser.Parse(XDocument.Load(fileName)); // Load the content of the specified file into Data
                data = null;
                Data = score;

                // Get the key signature
                keySig = (Key) score.FirstStaff.Elements.First(k => k.GetType() == typeof(Key));
                if (KeySig == null) KeySig = new Key(0);

                // Get the time signature
                timeSig = (TimeSignature) score.FirstStaff.Elements.First(k => k.GetType() == typeof(TimeSignature));
                if (timeSig == null) timeSig = TimeSignature.CommonTime;

                // Set up the back end data model
                staves = null;
                staves = new List<LStaff>();
                foreach (var staff in score.Staves)
                {
                    LStaff ls = null;
                    List<LMeasure> measures = new List<LMeasure>();
                    foreach (var measure in staff.Measures)
                    {
                        LMeasure m = new LMeasure(measure.Elements, ls, timeSig.WholeNoteCapacity);
                        measures.Add(m);
                    }
                    ls = new LStaff(staff, measures);
                    this.staves.Add(ls);
                }
                player = new MidiTaskScorePlayer(data);
                updateView();

            }
            else throw new FileNotFoundException(fileName + " not found."); //This and any parser exceptions will be caught by the calling function
        }


        /// <summary>
        /// Create new single staff score with the specified key and time signature.
        /// </summary>
        /// <param name="key">The key signature</param>
        /// <param name="timeSig">The time signature</param>
        public void createNew(Key key, TimeSignature timeSig)
        {
            Score score = Score.CreateOneStaffScore();
            MusicalSymbol[] elements = { Clef.Treble, key, timeSig }; // Add treble clef, key sig, time sig
            staves = new List<LStaff>();
            staves.Add(new LStaff(score.FirstStaff, Clef.Treble, key, timeSig));
            for (int i = 0; i < 3; i++) score.FirstStaff.Elements.Add(elements[i]);
            Data = null;    // If there is an existing score, reset it to null
            Data = score;
        }


        /// <summary>
        /// Saves the current Score as a MusicXML file.
        /// </summary>
        public void save()
        {
            // Create a save dialog to get the output file path
            Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog();
            if (fileName == "" || !File.Exists(fileName))
            {
                dialog.DefaultExt = "mml";
                dialog.CheckPathExists = true;
                dialog.ValidateNames = true;
                dialog.Filter = "Music XML|*.mml";
                Nullable<bool> result = dialog.ShowDialog();
                if (result == false) throw new FileNotFoundException();
                fileName = dialog.FileName;
            }

            try 
            {
                // Put the staff in a part to meet MusicXML schema requirements
                Part p1 = new Part(data.FirstStaff);
                p1.Name = "FirstPart";
                p1.PartId = "01";
                PartGroup pg1 = new PartGroup();
                p1.Group = pg1;
                data.PartGroups.Add(pg1);
                data.Parts.Add(p1);

                // Convert to MusicXML and save
                var parser = new MusicXmlParser();
                var outputXml = parser.ParseBack(data);
                outputXml.Save(fileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not save to " + fileName + ".\n" + ex.Message);
                fileName = "";
                save();
            }
        }


        /// <summary>
        /// Forces the Score object to refresh its property tree. Used
        /// to update the content of the bound NoteViewer.
        /// </summary>
        public void updateView()
        {
            var score = Data;
            Data = null;
            Data = score;
        }


        /// <summary>
        /// Plays the current note through the default MIDI channel.
        /// </summary>
        /// <param name="note"></param>
        public void PlayNote(Note note)
        {
            if (player == null) player = new MidiTaskScorePlayer(data);
            player.PlayElement(note);
        }

        /// <summary>
        /// Resets the MidiScoreTaskPlayer to a clean state.
        /// </summary>
        public void ResetPlayer()
        {
            if (player != null) ((IDisposable)player).Dispose();
            player = null;
            player = new MidiTaskScorePlayer(data);
        }

        /// <summary>
        /// Cleans up resources.
        /// </summary>
        /// <param name="cleanAll">true to explicitly dispose managed resources; false otherwise.</param>
        protected virtual void Dispose(bool cleanAll)
        {
            player = null;
            if (cleanAll) data = null;            
        }

        /// <summary>
        /// Cleans up resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
    ///////// CLASSES PROVIDED BY MANUFACTURA DOCUMENTATION //////////

    /// <summary>
    /// Abstract class for commands that deal with playback.
    /// </summary>
    public abstract class PlayerCommand : System.Windows.Input.ICommand
    {
        public event EventHandler CanExecuteChanged;
        protected ScoreVM viewModel;

        protected PlayerCommand(ScoreVM viewModel)
        {
            this.viewModel = viewModel;
        }

        public void FireCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, new EventArgs());
        }

        public abstract bool CanExecute(object parameter);
        public abstract void Execute(object parameter);
    }

    /// <summary>
    /// Command to handle Play and Pause functions.
    /// </summary>
    public class PlayCommand : PlayerCommand
    {
        public PlayCommand(ScoreVM viewModel) : base(viewModel) { }
        
        public override bool CanExecute(object parameter)
        {
            return viewModel.Player != null;
        }

        public override void Execute(object parameter)
        {
            if (viewModel.Player.CurrentElement == viewModel.Data.FirstStaff.Elements[viewModel.Data.FirstStaff.Elements.Count - 1])
            {
                viewModel.Player?.Stop();
            }

            if (viewModel.Player.State == ScorePlayer.PlaybackState.Playing) viewModel.Player.Pause();
            else viewModel.Player?.Play();
        }
    }

    /// <summary>
    /// Command to stop playback.
    /// </summary>
    public class StopCommand : PlayerCommand
    {
        public StopCommand(ScoreVM viewModel) : base(viewModel) { }

        public override bool CanExecute(object parameter)
        {
            return viewModel.Player != null;
        }

        public override void Execute(object parameter)
        {
            viewModel.Player?.Stop();
        }
    }
}
