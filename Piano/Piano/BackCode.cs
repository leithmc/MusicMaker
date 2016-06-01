using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Piano
{
    class BackCode
    {
        public bool LoadFile(string fileName)
        {

            return true;
        }

        public bool CreateNew(string fileName, Staff[] staves)
        {

            return true;
        }

    }

    public class Staff
    {
        private KeySignature keySig;
        private TimeSignature timeSig;
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
