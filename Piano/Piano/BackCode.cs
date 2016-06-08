﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.IO;

namespace Piano
{
    class BackCode
    {
        //static XmlDocumentType DocType = new XmlDocumentType
        private static bool isValid = true;

        /// <summary>
        /// Loads the specified XML document. 
        /// </summary>
        /// <param name="fileName">The name, including path, of the MusicXML file to load.</param>
        /// <returns>An XmlDocument object containing the contents of the specified file.</returns>
    //    public static XmlDocument LoadFile(string fileName)
    //    {
    //        XmlDocument doc;
    //        doc = new XmlDocument();
    //        if (File.Exists(fileName))
    //        {
    //            // Validate file before loading
    //            XmlTextReader r = new XmlTextReader("C:\\MyFolder\\ProductWithDTD.xml");
    //            XmlValidatingReader v = new XmlValidatingReader(r);


    //        }
    //        doc.Load(fileName);
    //        doc.CreateDocumentType()
    //        return doc;
    //    }

    //    public XmlDocument CreateNew(string fileName, Staff[] staves)
    //    {

    //        return true;
    //    }

    }

    public class Staff
    {
        private KeySignature keySig;
        private TimeSignature timeSig;
        private Cleff cleff;
        public Staff(KeySignature keySig, TimeSignature timeSig, Cleff cleff)
        {

        }

        public KeySignature KeySig
        {
            get
            {
                return keySig;
            }

            set
            {
                keySig = value;
            }
        }

        public TimeSignature TimeSig
        {
            get
            {
                return timeSig;
            }

            set
            {
                timeSig = value;
            }
        }

        public Cleff Cleff
        {
            get
            {
                return cleff;
            }

            set
            {
                cleff = value;
            }
        }
    }

    public class TimeSignature
    {
        private int beatsPerMeasure, beatType;
        public TimeSignature(int beatsPerMeasure, int beatType)
        {
            this.beatsPerMeasure = beatsPerMeasure;
            this.beatType = beatType;
        }
        
        public int BeatType
        {
            get { return beatType; }
        }

        public int BeatsPerMeasure
        {
            get { return beatsPerMeasure; }
        }

        public override string ToString() { return beatsPerMeasure + "/" + beatType; }
    }

    public enum KeySignature { F, C, G, D, A, E, B, Bb, Eb, Ab, Db, Gb, Cb, Fs, Cs, Gs, Ds, As, Es, Bs  };
    public enum Cleff { Treble, Bass, Alto, Tenor };
}

