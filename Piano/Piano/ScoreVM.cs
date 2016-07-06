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
        private static bool isPaused;
        private static bool isPlaying;
        private static bool isLooped;
        private static bool stopping;

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

            // Put the one staff in a part IDEALLY THIS SHOULD BE HERE BUT IT HAS TO BE IN SAVE SO THE PARTS DON"T DISAPPEAR
            //Part p1 = new Part(score.FirstStaff);
            //p1.Name = "FirstPart";
            //p1.PartId = "01";
            //PartGroup pg1 = new PartGroup();
            //p1.Group = pg1;
            //score.PartGroups.Add(pg1);
            //score.Parts.Add(p1);


            // Add treble clef
            score.FirstStaff.Elements.Add(Clef.Treble);
            // add key signature. The 0 in the Key constructor means no sharps or flats. 
            // negative numbers are flat keys, positive for sharp keys.
            keySig = new Key(0);
            score.FirstStaff.Elements.Add(keySig);
            // Set time sig to 4/4
            timeSig = TimeSignature.CommonTime;
            score.FirstStaff.Elements.Add(timeSig);
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
            for (int i = 0; i < 3; i++) score.FirstStaff.Elements.Add(elements[i]);

            // See if this fixes wrap around
            foreach (var system in score.Systems)
            {
                system.Width = 4;
            }

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

            try    // MusicXML format (not yet implemented in Manufactura)
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


        // EXPERIMENTAL
        internal NoteOrRest fitMeasures(Measure editedMeasure) // Still need to add support for deletion
        {
            Staff staff = editedMeasure.Staff;
            NoteOrRest newCursor = null;
            
            // Last measure case
            if (editedMeasure.Number == staff.Measures.Count)
            {
                double overage = getOverage(editedMeasure);
                if (overage >= 0)
                    staff.Elements.Add(new Barline());
                if (overage <= 0) return null;
            }

            bool done = false;
            for (int i = staff.Measures.IndexOf(editedMeasure); i < staff.Measures.Count; i++)
            {
                Measure m = staff.Measures[i];
                if (getOverage(m) == 0) break;

                // Deletion case
                while (getOverage(m) < 0 && canStealNotes(m))
                {
                    stealNoteOrRest(m);
                }

                // Insertion case
                while (getOverage(m) >= 0)
                {
                    //if (m.Number == staff.Measures.Count) staff.Elements.Add(new Barline());
                    double overage = getOverage(m);
                    if (overage == 0) break;

                    NoteOrRest lastNoteOrRest = (NoteOrRest) m.Elements.Last(e => e.GetType().IsSubclassOf(typeof(NoteOrRest)));
                    NoteOrRest itemToMove, itemInPlace = null;   // The last note or portion thereof that doesn't fit in the current measure
                    double lastDurationValue = lastNoteOrRest.Duration.ToDouble();  // Value of last note or rest in measure
                    int lastItemIndex = staff.Elements.IndexOf(lastNoteOrRest); // Index of last note or rest in the measure

                    if (lastNoteOrRest.GetType() == typeof(Note))
                    {
                        if (lastDurationValue > overage) itemInPlace = new Note(((Note)lastNoteOrRest).Pitch, toRhythmicDuration(lastDurationValue - overage));
                        itemToMove = new Note(((Note)lastNoteOrRest).Pitch, toRhythmicDuration(overage)); // Remaining note value
                    }
                    else
                    {
                        if (lastDurationValue > overage) itemInPlace = new Rest(toRhythmicDuration(lastDurationValue - overage));
                        itemToMove = new Rest(toRhythmicDuration(overage));
                    }

                    staff.Elements.Remove(lastNoteOrRest);
                    m.Elements.Remove(lastNoteOrRest);

                    if (itemInPlace != null)
                    {
                        staff.Elements.Insert(lastItemIndex, itemInPlace); //If trouble, switch to insertElement
                    }

                    if (m.Number == staff.Measures.Count)
                    {
                        m.Elements.Add(new Barline());
                        staff.Elements.Add(itemToMove);
                    }

                    else
                    {
                        int destIndexInStaff = staff.Elements.IndexOf(staff.Measures[i + 1].Elements[0]);
                        insertElement(staff, destIndexInStaff, itemToMove);
                    }
                    //getNextMeasure(m).Elements.Insert(0, itemToMove);
                    if (m == editedMeasure) newCursor = itemToMove;
                }
            }
            //breakStaffIfNeeded();
            return newCursor; 
        }

        // Replaces the buggy Staff.Elements.Insert method
        private void insertElement(Staff staff, int index, MusicalSymbol elem)
        {
            staff.Elements.Insert(index, elem);
            staff.Measures.Last().Elements.Remove(elem);

            var prevElem = staff.Elements[index - 1];
            Measure prevElemMeasure = prevElem.Measure;
            Measure targetMeasure;
            int indexInMeasure;
            if (prevElem.GetType() == typeof(Barline))
            {
                targetMeasure = staff.Measures[staff.Measures.IndexOf(prevElemMeasure) + 1];
                indexInMeasure = 0;
            }
            else
            {
                targetMeasure = prevElemMeasure;
                indexInMeasure = prevElemMeasure.Elements.IndexOf(prevElem) + 1;
            }

            targetMeasure.Elements.Insert(indexInMeasure, elem);
        }

        private bool canStealNotes(Measure m)
        {
            Measure last = m.Staff.Measures[m.Staff.Measures.Count - 1];
            if (m.Number == 1
                || m == last
                || last == null
                || last.Elements == null
                || !last.Elements.Any(e => e.GetType().IsSubclassOf(typeof(NoteOrRest))))
                return false;
            else return true;
        }




        /// <summary>
        /// Adds a barline to the current measure if needed, and breaks the last note of the measure if it
        /// exceeds the allowed beats/measure, inserting the remainder into the next measure. Then recursively calls
        /// itself on subsequent measures until it hits a measure that does not overflow.
        /// </summary>
        /// <param name="m"></param>
        /// <param name="ts"></param>
        //internal void fitMeasureOldVersion(Measure m, TimeSignature ts) // Still need to add support for deletion
        //{
        //    bool nextMeasureChanged = false;
        //    Staff staff = m.Staff;
        //    int measureIndex = staff.Measures.IndexOf(m);
            

        //    #region TimeSigs
        //    foreach (MusicalSymbol item in m.Elements)
        //    {
        //        // If new time signature, update
        //        if (item.GetType() == typeof(TimeSignature)) ts = (TimeSignature)item;

        //        // Make sure that any cleffs and signatures are at the beginning of the measure, before notes and rests
        //        if (item.GetType() == typeof(TimeSignature) || item.GetType() == typeof(Key) || item.GetType() == typeof(Clef))
        //        {
        //            NoteOrRest firstBeat = getFirstBeat(m);
        //            if (m.Elements.IndexOf(firstBeat) < m.Elements.IndexOf(item)) swapPositions(staff, item, firstBeat);
        //        }
        //    }
        //    #endregion

        //    #region deletion
        //    // While the current measure is not full and is not the end of the song, steal the first beat from the next measure
        //    while (getDurations(m).Sum() < ts.WholeNoteCapacity
        //        && m.Number < staff.Measures.Count
        //        && getFirstBeat(getNextMeasure(m)) != null)
        //    {
        //        Measure next = getNextMeasure(m);
        //        NoteOrRest nr = getFirstBeat(next);
        //        int destIndex = staff.Elements.IndexOf(m.Elements.Last(e => e.GetType().IsSubclassOf(typeof(NoteOrRest)))) + 1;
        //        stealNoteOrRest(nr, destIndex);
        //        nextMeasureChanged = true;
        //    } 
        //    #endregion

        //    //**********THIS STILL NEEDS SOME WORK******
        //    //    Insertion is putting barlines in funny places
        //    // I think the recursion is putting barlines progressively closer together

        //    // While the current measure has too many beats, push the extra into the next measure
        //    while (getDurations(m).Sum() > ts.WholeNoteCapacity)
        //    {
        //        double overage = getOverage(m, ts);     // How much the content of the current measure exceeds its alloted time
        //        NoteOrRest lastNoteOrRest = (NoteOrRest)m.Elements.Last(e => e.GetType().IsSubclassOf(typeof(NoteOrRest))); // Last note or rest in measure
        //        double lastDurationValue = lastNoteOrRest.Duration.ToDouble();  // Value of last note or rest in measure
        //        int lastItemIndex = staff.Elements.IndexOf(lastNoteOrRest); // Index of last note or rest in the measure
        //        NoteOrRest itemToMove = null;   // The last note or portion thereof that doesn't fit in the current measure

        //        // If the overage is because the last note is too big, break it into two notes
        //        if (lastDurationValue > overage)
        //        {
        //            NoteOrRest itemInPlace;     // Shortened version of lastNoteOrRest that fits in currrent measure
        //            if (lastNoteOrRest.GetType() == typeof(Note))
        //            {
        //                itemInPlace = new Note(((Note)lastNoteOrRest).Pitch, toRhythmicDuration(lastDurationValue - overage));
        //                itemToMove = new Note(((Note)lastNoteOrRest).Pitch, toRhythmicDuration(overage)); // Remaining note value
        //            }
        //            else
        //            {
        //                itemInPlace = new Rest(toRhythmicDuration(lastDurationValue - overage));
        //                itemToMove = new Rest(toRhythmicDuration(overage));
        //            }
        //            // Have to remove lastNoteOrRest from both the staff and the measure
        //            staff.Elements.Remove(lastNoteOrRest);
        //            m.Elements.Remove(lastNoteOrRest);
        //            // Replace with shortened version, automatically populates to measure when adding
        //            staff.Elements.Insert(lastItemIndex, itemInPlace);
        //        }
        //        else
        //        {
        //            itemToMove = lastNoteOrRest;
        //            staff.Elements.Remove(lastNoteOrRest);
        //            m.Elements.Remove(lastNoteOrRest);
        //            lastItemIndex--;
        //        }

        //        // Refresh the updated measure in the staff.measures collection
        //        Barline bar = moveOrAddBarlineAfter(staff.Elements[lastItemIndex]);

        //        // Add itemToMove after the barline
        //        staff.Elements.Insert(lastItemIndex + 2, itemToMove);

        //        // Add it to the measure as well if it's not already there
        //        Measure nextMeasure = getNextMeasure(m);
        //        if (!nextMeasure.Elements.Contains(itemToMove)) nextMeasure.Elements.Insert(0, itemToMove);

        //        // set the flag to check the next measure
        //        nextMeasureChanged = true;
        //    }

        //    // If there are changes to the next measure, fix it.
        //    if (nextMeasureChanged) fitMeasures(getNextMeasure(m), ts);
        //}

        // Removes the first note or rest from the next measure and adds it to the current measure.
        private void stealNoteOrRest(Measure m)
        {
            Staff staff = m.Staff;
            Measure nextMeasure = getNextMeasure(m);
            NoteOrRest itemToSteal = getFirstBeat(nextMeasure);
            NoteOrRest lastBeat = (NoteOrRest) m.Elements.Last(e => e.GetType().IsSubclassOf(typeof(NoteOrRest)));
            int insertionPoint = staff.Elements.IndexOf(lastBeat) + 1;

            m.Elements.Remove(itemToSteal);
            staff.Elements.Remove(itemToSteal);
            //dest.Elements.Insert(destIndexInMeasure, nr);
            staff.Elements.Insert(insertionPoint, itemToSteal);
        }

        // Moves the next barline to the position immediately after the specified element. If there are no more barlines on
        // the staff, creates a new one.
        private Barline moveOrAddBarlineAfter(MusicalSymbol item)
        {
            if (item.GetType() == typeof(Barline)) return (Barline)item;
            var elems = item.Staff.Elements;
            int itemPosition = elems.IndexOf(item);
            Barline bar = (Barline) elems.FirstOrDefault(b => elems.IndexOf(b) > itemPosition && b.GetType() == typeof(Barline));
            if (bar == null) bar = new Barline();
            else item.Staff.Elements.Remove(bar);
            item.Staff.Elements.Insert(itemPosition + 1, bar);
            return bar;
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

        // Returns the total beat durations of notes and rests in the measure minus the allowed amount
        private double getOverage(Measure m) {  return getDurations(m).Sum() - timeSig.WholeNoteCapacity; }

        // Returns the last note or rest in the measure
        private NoteOrRest getLast(Measure m)
        {
            for (int i = m.Elements.Count - 1; i >= 0; i--)
                if (m.Elements[i].GetType().IsSubclassOf(typeof(NoteOrRest))) return (NoteOrRest) m.Elements[i];
            return null;
        }

        // Helper method to switch positions of elements on a staff
        private void swapPositions(Staff staff, MusicalSymbol item1, MusicalSymbol item2)
        {
            int pos1 = staff.Elements.IndexOf(item1);
            int pos2 = staff.Elements.IndexOf(item2);
            staff.Elements.Remove(item1);
            staff.Elements.Insert(pos2, item1);
            staff.Elements.Remove(item2);
            staff.Elements.Insert(pos1, item2);
        }

        // Helper method to convert a double to a RhythmicDuration object.
        private RhythmicDuration toRhythmicDuration(double d)
        {
            RhythmicDuration rd;
            // Check fractions down to 1/32 and convert to the base note type
            if (d >= 1) rd = RhythmicDuration.Whole;
            else if (d >= .5) rd = RhythmicDuration.Half;
            else if (d >= .25) rd = RhythmicDuration.Quarter;
            else if (d >= .125) rd = RhythmicDuration.Eighth;
            else if (d >= .0625) rd = RhythmicDuration.Sixteenth;
            else rd = RhythmicDuration.D32nd;

            // Fill in dots if needed
            while (DottedValue(rd) < d)
            {
                var v = DottedValue(rd);
                rd.Dots++;
            }

            return rd;
        }

        private double DottedValue(RhythmicDuration rd)
        {
            double d = rd.WithoutDots.ToDouble();
            double dot = d / 2;
            for (int i = 0; i < rd.Dots ; i++)
            {
                d += dot;
                dot = dot / 2;
            }
            return d;
        }

        public async Task Play()
        {
            stopping = false;
            if (player != null)
            {
                ((IDisposable)player).Dispose();
                player = null;
            }
            player = new MidiTaskScorePlayer(data);
            isPlaying = true;
            player.Play();
            while (!stopping && player.CurrentElement != data.FirstStaff.Elements[data.FirstStaff.Elements.Count - 1])
            {
                System.Threading.Thread.Sleep(1);
            }
            player.Stop();
        }

        public void Pause()
        {
            player?.Pause();
            isPaused = true;
        }

        public async Task Resume()
        {
            stopping = false;
            isPaused = false;
            player.Play();
            while (!stopping && player.CurrentElement != data.FirstStaff.Elements[data.FirstStaff.Elements.Count - 1])
            {
                System.Threading.Thread.Sleep(1);
            }
            isPlaying = false;
            ((IDisposable)player).Dispose();
        }

        public void Stop()
        {
            //player?.Stop();
            stopping = true;
            //isPlaying = false;
          //  isPaused = false;
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
