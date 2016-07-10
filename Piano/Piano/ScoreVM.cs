using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Manufaktura.Model;
using Manufaktura.Controls;
using Manufaktura.Controls.WPF;
using Manufaktura.Music;
using Manufaktura.Controls.Model;
using Manufaktura.Model.MVVM;
using Manufaktura.Music.Model;
using Manufaktura.Music.Model.MajorAndMinor;
using System.IO;
using Manufaktura.Controls.Parser;
using System.Xml;
using System.Xml.Linq;
using System.Windows;
using System.Runtime.Serialization.Formatters.Binary;
using Manufaktura.Controls.Audio;
using Manufaktura.Controls.Desktop.Audio;
using Manufaktura.Controls.Desktop;
using LData;

namespace Piano
{
    public class ScoreVM : ViewModel
    {
        private string fileName = "";   // Name of the file to load from or save to.
        private ScorePlayer player;
        public ScorePlayer Player => player;

        public PlayCommand PlayCommand { get; }
        public StopCommand StopCommand { get; }

        private Score data;

        internal List<LStaff> staves;

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
                //if (player != null) ((IDisposable)player).Dispose(); //This is needed in Midi player. Otherwise it can throw a "Device not ready" exception.
                //if (data != null) player = new MidiTaskScorePlayer(data);
                OnPropertyChanged();
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

            // SHIFT TO LSTAVES
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
            score.FirstStaff.Elements.Add(new Note(Pitch.C5, RhythmicDuration.Quarter));
            score.FirstStaff.Elements.Add(new Note(Pitch.B4, RhythmicDuration.Quarter));
            score.FirstStaff.Elements.Add(new Note(Pitch.C5, RhythmicDuration.Half));
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
                Data = score;
                keySig = (Key) score.FirstStaff.Elements.First(k => k.GetType() == typeof(Key));
                if (KeySig == null) KeySig = new Key(0);
                timeSig = (TimeSignature) score.FirstStaff.Elements.First(k => k.GetType() == typeof(TimeSignature));
                if (timeSig == null) timeSig = TimeSignature.CommonTime;

                // Set up the back end data model
                this.staves = new List<LStaff>();
                foreach (var staff in score.Staves)
                {
                    LStaff ls = new LStaff(staff);
                    foreach (var measure in staff.Measures)
                    {
                        LMeasure m = new LMeasure(measure.Elements, ls, timeSig.WholeNoteCapacity);
                        ls.AddLast(m);
                    }
                    this.staves.Add(ls);
                }
            }
            else throw new FileNotFoundException(fileName + " not found."); //This and any parser exceptions will be caught by the calling function
        }

        /// <summary>
        /// Creates a new score with the specified staff configuration and loads it into the viewer.
        /// </summary>
        /// <param name="title">The title of the piece</param>
        /// <param name="staves">An array of Staff objects representing the different parts in the composition.</param>
        public void createNew(string title, Staff[] staves)
        {
            Score score = new Score();
            foreach (Staff staff in staves)
            {
                score.Staves.Add(staff);
            }
            Data = score;
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
 
            // See if this fixes wrap around
            //foreach (var system in score.Systems)
            //{
            //    system.Width = 4;
            //}

            Data = null;
            Data = score;
            //updateView();
        }


        /// <summary>
        /// Saves the current Score as a MusicXML file.
        /// </summary>
        /// <returns></returns>
        public bool save()
        {
            var setting = data.FirstStaff.MeasureAddingRule;

            Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog();
            if (fileName == "" || !File.Exists(fileName))
            {
                dialog.DefaultExt = "mml";
                dialog.CheckPathExists = true;
                dialog.ValidateNames = true;

                dialog.Filter = "Music XML|*.mml";


                Nullable<bool> result = dialog.ShowDialog();
                if (result == false) return false;
                fileName = dialog.FileName;
            }

            try    // MusicXML format
            {
                // Put the one staff in a part
                Part p1 = new Part(data.FirstStaff);
                p1.Name = "FirstPart";
                p1.PartId = "01";
                PartGroup pg1 = new PartGroup();
                p1.Group = pg1;
                data.PartGroups.Add(pg1);
                data.Parts.Add(p1);


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

            return true;
        }


        /// <summary>
        /// Forces the Score object to refresh its property tree. Have to do this
        /// to update the content of the bound NoteViewer.
        /// </summary>
        public void updateView()
        {
            var score = Data;
            Data = null;
            Data = score;
        }


        // Helper methods to get the next measure and the first note or rest in a measure
        private Measure getNextMeasure(Measure m) { return m.Staff.Measures.Find(m2 => m2.Number == m.Number + 1); }
        private NoteOrRest getFirstBeat(Measure m) { return (NoteOrRest) m.Elements.Find(e => e.GetType().IsSubclassOf(typeof(NoteOrRest))); }

        // Get an array of doubles that represent the durations of all elements in a measure
        private double[] getDurations(Measure m)
        {
            double[] d = new double[m.Elements.Count];
            for (int i = 0; i < d.Length; i++)
            {
                var elem = m.Elements[i];
                if (elem.GetType().IsSubclassOf(typeof(NoteOrRest))) 
                    d[i] = ((NoteOrRest)elem).Duration.ToDouble();
                else d[i] = 0;
            }
            return d;
        }

        // Puts line breaks in every 4th measure
        internal void breakStaffIfNeeded()
        {
            foreach (Staff staff in data.Staves)
            {
                StaffSystem s = staff.Measures[0].System;
                foreach (Measure m in staff.Measures)
                {
                    if (m.Number > 4 && (m.Number - 1) % 4 == 0) breakStaffAt(m);
                    //if (m.System.Width > 250) breakStaffAt(m);
                    else unbreakStaffAt(m);
                }
            }
        }

        internal void breakStaffAt(Measure m)
        {
            Measure previousMeasure = m.Staff.Measures[m.Staff.Measures.IndexOf(m) - 1];
            int insertionPoint = m.Staff.Elements.IndexOf(previousMeasure.Elements.Last()) + 1;
            if (m.Elements.Any(ms => ms.GetType() == typeof(PrintSuggestion))) return;
            var ps = new PrintSuggestion();
            ps.IsSystemBreak = true;
            m.Elements.Insert(0, ps);
            m.Staff.Elements.Insert(insertionPoint, ps);
        }

        internal void unbreakStaffAt(Measure m)
        {
            m.Elements.RemoveAll(e => e.GetType() == typeof(PrintSuggestion));
        }

        // Returns the last note or rest in the measure
        private NoteOrRest getLast(Measure m)
        {
            for (int i = m.Elements.Count - 1; i >= 0; i--)
                if (m.Elements[i].GetType().IsSubclassOf(typeof(NoteOrRest))) return (NoteOrRest) m.Elements[i];
            return null;
        }

        public void PlayNote(Note note)
        {
            if (player == null) player = new MidiTaskScorePlayer(data);
            player.PlayElement(note);
        }

        public void ResetPlayer()
        {
            if (player != null) ((IDisposable)player).Dispose();
            player = null;
            player = new MidiTaskScorePlayer(data);

        }

    }

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

    public class PlayCommand : PlayerCommand
    {

        public PlayCommand(ScoreVM viewModel) : base(viewModel)
        {
        }



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

    public class StopCommand : PlayerCommand
    {
        public StopCommand(ScoreVM viewModel) : base(viewModel)
        {
        }

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
