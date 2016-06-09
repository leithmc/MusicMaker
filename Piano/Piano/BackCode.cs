using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.IO;
using Manufaktura.Controls.Parser;
using System.Xml.Linq;
using Manufaktura.Controls.Model;

namespace Piano
{
    class BackCode
    {
        //static XmlDocumentType DocType = new XmlDocumentType
        private static bool isValid = true;

        /// <summary>
        /// Loads the specified XML document.
        /// </summary>
        /// <param name = "fileName" > The name, including path, of the MusicXML file to load.</param>
        /// <returns>An XmlDocument object containing the contents of the specified file.</returns>
        public static Score LoadFile(string fileName)
        {
            if (File.Exists(fileName))
            {
                // Validate file before loading
                if (!validateMusicXML(fileName)) throw new FileFormatException("File: " + fileName + " is not a valid MusicXML file.");
                var parser = new MusicXmlParser();
                Score score = parser.Parse(XDocument.Load(fileName));
                return score;
            }
            throw new FileNotFoundException(fileName + " not found.");
        }


        /// <summary>
        /// Validates that the file at the specified path matches the MusicXML schema definition.
        /// </summary>
        /// <param name="fileName">The name of the file to validate, including path.</param>
        /// <returns>true if valid; false otherwise.</returns>
        private static bool validateMusicXML(string fileName)
        {
            // To be implemented
            //XmlTextReader r = new XmlTextReader("C:\\MyFolder\\ProductWithDTD.xml");
            //XmlValidatingReader v = new XmlValidatingReader(r);
            string s = fileName;
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName">The name of the file to validate, including path.</param>
        /// <param name="staves">An array of Staff objects representing the different parts in the composition.</param>
        /// <returns>An XmlDocument containing the empty score.</returns>
        public Score CreateNew(string fileName, Staff[] staves)
        {
            // To be implemented
            return new Score();
        }

    }

    //public class Staff
    //{
    //    private KeySignature keySig;
    //    private TimeSignature timeSig;
    //    private Cleff cleff;
    //    public Staff(KeySignature keySig, TimeSignature timeSig, Cleff cleff)
    //    {
    //        this.keySig = keySig;
    //        this.timeSig = timeSig;
    //        this.cleff = cleff;
    //    }

    //    public KeySignature KeySig
    //    {
    //        get { return keySig; }
    //        set { keySig = value; }
    //    }

    //    public TimeSignature TimeSig
    //    {
    //        get { return timeSig; }
    //        set { timeSig = value; }
    //    }

    //    public Cleff Cleff
    //    {
    //        get { return cleff; }
    //        set { cleff = value; }
    //    }
    //}

    //public class TimeSignature
    //{
    //    private int beatsPerMeasure, beatType;
    //    public TimeSignature(int beatsPerMeasure, int beatType)
    //    {
    //        this.beatsPerMeasure = beatsPerMeasure;
    //        this.beatType = beatType;
    //    }
        
    //    public int BeatType
    //    {
    //        get { return beatType; }
    //    }

    //    public int BeatsPerMeasure
    //    {
    //        get { return beatsPerMeasure; }
    //    }

    //    public override string ToString() { return beatsPerMeasure + "/" + beatType; }
    //}

    //public enum KeySignature { F, C, G, D, A, E, B, Bb, Eb, Ab, Db, Gb, Cb, Fs, Cs, Gs, Ds, As, Es, Bs  };
    //public enum Cleff { Treble, Bass, Alto, Tenor };
}

