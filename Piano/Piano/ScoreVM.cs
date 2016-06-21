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

namespace Piano
{
    public class ScoreVM : ViewModel
    {
        private string fileName = "";   // Name of the file to load from or save to.

        private Score data;
        /// <summary>
        /// Holds the current Score object. Refreshing the public property updates the viewer.
        /// </summary>
        public Score Data
        {
            get { return data; }
            set { data = value; OnPropertyChanged(() => Data); }
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

        /// <summary>
        /// Populates the note viewer with an empty single staff. Does not set fileName.
        /// </summary>
        public void loadStartData()
        {
            Data = createStartingStaff();   // This will switch to createGrandStaff once the note entry bugs are fixed
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
            // Add some notes for test purposes
            score.FirstStaff.Elements.Add(new Note(Pitch.C5, RhythmicDuration.Quarter));
            score.FirstStaff.Elements.Add(new Note(Pitch.B4, RhythmicDuration.Quarter));
            score.FirstStaff.Elements.Add(new Note(Pitch.C5, RhythmicDuration.Half));
            score.FirstStaff.Elements.Add(new Barline());
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
        public Score createGrandStaff() { return createGrandStaff(new Key(0), TimeSignature.CommonTime); }

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
            foreach (Staff staff in staves) { score.Staves.Add(staff); }
            // TODO: Figure out how to add a title. This may have to be done directly to the MusicXML while saving.
            // Replace existing socre object with newly defined score
            Data = score;
        }


        /// <summary>
        /// Saves the current Score as a MusicXML file. NOT FINISHED YET
        /// </summary>
        /// <returns></returns>
        public bool save()
        {
            if (fileName == "" || !File.Exists(fileName))
            {
                Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog();
                dialog.DefaultExt = "mml";
                dialog.CheckPathExists = true;
                dialog.ValidateNames = true;
                Nullable<bool> result = dialog.ShowDialog();
                if (result == false) return false;
                fileName = dialog.FileName;
            }

            try
            {
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

        ////// IN PROGRESS ////////
        /// <summary>
        /// Adds a barline to the current measure if needed, and breaks the last note of the measure if it
        /// exceeds the allowed beats/measure, inserting the remainder into the next measure, then recursively calls
        /// itself on subsequent measures until it hits a measure that does not overflow.
        /// </summary>
        /// <param name="m"></param>
        /// <param name="ts"></param>
        internal void fitMeasure(Measure m, TimeSignature ts) // Still need to add support for deletion
        {
            double beats = 0.0;
            foreach (MusicalSymbol item in m.Elements)//.FindAll(e => e.GetType() == typeof(NoteOrRest)))
            {
                // If new time sig, update
                if (item.GetType() == typeof(TimeSignature)) ts = (TimeSignature) item;

                else if (item.GetType() == typeof(Note) || item.GetType() == typeof(Rest))
                {
                    NoteOrRest nr = (NoteOrRest)item;
                    double d = nr.Duration.ToDouble();
                    double overage = (beats + d) - ts.WholeNoteCapacity;
                    int index = m.Staff.Elements.IndexOf(item);
                    if (overage > 0)
                    {
                        double d1 = d - overage;
                        RhythmicDuration dur1 = getDuration(d1);
                        NoteOrRest firstPart, secondPart;
                        if (nr.GetType() == typeof(Note))
                        {
                            Note n = (Note)nr;
                            firstPart = new Note(n.Pitch, getDuration(d1));
                            secondPart = new Note(n.Pitch, getDuration(overage));
                        }
                        else
                        {
                            firstPart = new Rest(getDuration(d1));
                            secondPart = new Rest(getDuration(overage));
                        }
                        m.Staff.Elements.Insert(index, firstPart);    
                        m.Staff.Elements.Remove(item);
                        if (m.Staff.Elements.Count < index + 2 
                            || m.Staff.Elements[index + 1].GetType() != typeof(Barline))
                            m.Staff.Elements.Insert(index + 1, new Barline());
                        m.Staff.Elements.Insert(index + 2, secondPart);

                        // We had to fit this into a new measure, so make recursive call for the next measure.
                        fitMeasure(m.Staff.Elements[index + 2].Measure, ts);
                    }
                    else if (overage == 0) // right now it's creating a barline and pushing it to the end -- fix that
                    {
                        if (m.Staff.Elements.Count < index + 2
                            || m.Staff.Elements[index + 1].GetType() != typeof(Barline))
                            m.Staff.Elements.Insert(index + 1, new Barline());
                    }
                }
            }
        }

        // Helper method to convert a double to a RhythmicDuration object.
        private RhythmicDuration getDuration(double d)
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
            while (rd.ToDouble() < d) rd.Dots++;

            return rd;
        }

    }
}
