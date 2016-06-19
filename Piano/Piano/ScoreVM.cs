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

       // public void forceUpdate() { OnPropertyChanged(() => Data); }


        /// <summary>
        /// Populates the note viewer with an empty default grand staff. Does not set fileName.
        /// </summary>
        public void loadStartData()
        {
            Data = createStartingStaff();
        }

        /// <summary>
        /// Generates an empty grand staff in the specified key and time signature.
        /// </summary>
        /// <param name="key">A Key enumeration value to specify the starting key of the piece</param>
        /// <param name="timeSig">A TimeSignature object representing the time signature of the piece</param>
        /// <returns>A score object containing a grand staff in the specified key and time signature</returns>
        public Score createStartingStaff()
        {
            var score = Score.CreateOneStaffScore();
            score.FirstStaff.Elements.Add(Clef.Treble);
            score.FirstStaff.Elements.Add(new Key(0));
            score.FirstStaff.Elements.Add(TimeSignature.CommonTime);
            score.FirstStaff.Elements.Add(new Note(Pitch.C5, RhythmicDuration.Quarter));
            score.FirstStaff.Elements.Add(new Note(Pitch.B4, RhythmicDuration.Quarter));
            score.FirstStaff.Elements.Add(new Note(Pitch.C5, RhythmicDuration.Half));
            score.FirstStaff.Elements.Add(new Barline());
            return score;
        }

        // Do not delete -- will want to put this back in later
        /// <summary>
        /// Generates an empty grand staff in the specified key and time signature.
        /// </summary>
        /// <param name="key">A Key enumeration value to specify the starting key of the piece</param>
        /// <param name="timeSig">A TimeSignature object representing the time signature of the piece</param>
        /// <returns>A score object containing a grand staff in the specified key and time signature</returns>
        //public Score createGrandStaff(Key key, TimeSignature timeSig)
        //{
        //    var score = Score.CreateOneStaffScore();
        //    score.FirstStaff.Elements.Add(Clef.Treble);
        //    score.FirstStaff.Elements.Add(key);
        //    score.FirstStaff.Elements.Add(timeSig);
        //    score.FirstStaff.Elements.Add(new Note(Pitch.C5, RhythmicDuration.Quarter));
        //    score.FirstStaff.Elements.Add(new Note(Pitch.B4, RhythmicDuration.Quarter));
        //    score.FirstStaff.Elements.Add(new Note(Pitch.C5, RhythmicDuration.Half));
        //    score.FirstStaff.Elements.Add(new Barline());
        //    Staff bass = new Staff();
        //    bass.Elements.Add(Clef.Bass);
        //    bass.Elements.Add(key);
        //    bass.Elements.Add(timeSig);
        //    score.Staves.Add(bass);
        //    return score;
        //}

            /// keep this for later
        /// <summary>
        /// Returns a default empty grand staff.
        /// </summary>
        /// <returns>A Score object containing an empty grand staff in C major and 4/4 time</returns>
        //public Score createGrandStaff() { return createGrandStaff(new Key(0), TimeSignature.CommonTime); }

        /// <summary>
        /// Loads the viewer with a Score object generated from the specified MusicXml file.
        /// </summary>
        public void loadFile(string fileName)
        {
            this.fileName = fileName;
            if (File.Exists(fileName))
            {
                // Validate file before loading
                //if (!validateMusicXML(fileName)) throw new FileFormatException("File: " + fileName + " is not a valid MusicXML file.");
                var parser = new MusicXmlParser();
                Score score = parser.Parse(XDocument.Load(fileName));
                Data = score;
            }
            else throw new FileNotFoundException(fileName + " not found.");
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
            Data = score;
        }


        /// <summary>
        /// Saves the current Score as a MusicXML file.
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

        internal void addNote(Note note)
        {
            //var score = Score.CreateOneStaffScore();
            //score.FirstStaff.Elements.AddRange(data.FirstStaff.Elements);
            //score.FirstStaff.Elements.Add(note);
            //Data = score;
            Data.FirstStaff.Elements.Add(note);
           // Data.FirstStaff.Elements.Add(new Note(Pitch.C5, RhythmicDuration.Half));
            updateView();
        }

        public void updateView()
        {
            var score = Data;
            Data = null;
            Data = score;
        }

        internal void fitMeasure(Measure m, TimeSignature ts) // Still need to add support for deletion
        {
            double beats = 0.0;
            foreach (MusicalSymbol item in m.Elements)//.FindAll(e => e.GetType() == typeof(NoteOrRest)))
            {
                // If new time sig, update
                if (item.GetType() == typeof(TimeSignature)) ts = (TimeSignature) item;

                else if (item.GetType() == typeof(NoteOrRest))
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
                    else if (overage == 0)
                    {
                        if (m.Staff.Elements.Count < index + 2
                            || m.Staff.Elements[index + 1].GetType() != typeof(Barline))
                            m.Staff.Elements.Insert(index + 1, new Barline());
                    }
                }
            }
        }

        private RhythmicDuration getDuration(double d)
        {
            RhythmicDuration rd;
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
//////// Trim this down to single staff and simplify methods accordingly, then have another go at note input.