using Manufaktura.Controls.Model;
using Manufaktura.Music.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LData
{
    /// <summary>
    /// The LMeasure class represents a measure of music on a single staff. It may contain notes, rests, and other elements, 
    /// and includes methods to add and remove them. The LMeasure class automatically flows and breaks notes and rests as 
    /// needed across bar lines and into neighboring measures in the staff as they are added and removed.
    /// This is a substitute class for Manufaktura.Controls.Model.Measure.
    /// </summary>
    class LMeasure : LinkedList<MusicalSymbol>
    {
        double capacity = 1;
        private LStaff staff;

        //////////// Constructors ////////////
        /// <summary>
        /// Creates an LMeasure object that contains the supplied list of elements.
        /// </summary>
        /// <param name="elems">A LinkedList of MusicalSymbol objects to add to the LMeasure.</param>
        /// <param name="staff">The LStaff object that contains or will contain the LMeasure.</param>
        /// <param name="capacity">The number of whole notes that can fit into the measure. This number is overridden
        /// if the LMeasure contains a TimeSignature object. Default = 1.</param>
        internal LMeasure(List<MusicalSymbol> elems, LStaff staff, double capacity = 1)
        {
            foreach (MusicalSymbol item in elems) AddLast(item);

            if (capacity != 0) this.capacity = capacity;
            TimeSignature sig = (TimeSignature)elems.FirstOrDefault(e => e.GetType() == typeof(TimeSignature));
            if (sig != null) capacity = sig.WholeNoteCapacity;
            this.staff = staff;
            if (Beats > capacity) trimExtras();
        }

        /// <summary>
        /// Creates an LMeasure with a single child element.
        /// </summary>
        /// <param name="elem">A musical element to include at the beginning of the LMeasure.</param>
        /// <param name="staff">The LStaff object that contains or will contain the LMeasure.</param>
        /// <param name="capacity">The number of whole notes that can fit into the measure. This number is overridden
        /// if the LMeasure contains a TimeSignature object. Default = 1.</param>
        internal LMeasure(MusicalSymbol elem, LStaff staff, double capacity = 1) 
            : this(new List<MusicalSymbol>() { elem }, staff, capacity) { }

        /// <summary>
        /// Creates an empty LMeasure object.
        /// </summary>
        /// <param name="staff">The LStaff object that contains or will contain the LMeasure.</param>
        /// <param name="capacity">The number of whole notes that can fit into the measure. Default = 1.</param>
        internal LMeasure(LStaff staff, double capacity = 1) : this(new List<MusicalSymbol>(), staff, capacity) { }



        //////////// Public Properties ////////////////////////
        /// <summary>
        /// Returns a reference to the LStaff object that contains the current LMeasure.
        /// </summary>
        public LStaff Staff { get { return staff; } set { staff = value; } }

        /// <summary>
        /// Returns the LinkedListNode object containing the current LMeasure.
        /// </summary>
        public LinkedListNode<LMeasure> Node { get { return staff.Find(this); } }

        /// <summary>
        /// Returns the total whole note duration of notes and rests in the LMeasure.
        /// </summary>
        public double Beats
        {
            get
            {
                double beats = 0;
                foreach (NoteOrRest nr in this.Where(e => e.GetType().IsSubclassOf(typeof(NoteOrRest)))) beats += nr.Duration.ToDouble();
                return beats;
            }
        }

        /// <summary>
        /// Returns the whole note capacity of the LMeasure.
        /// </summary>
        public double Capacity {  get { return capacity; } }

        /// <summary>
        /// Returns the 1-based measure number of the current LMeasure.
        /// </summary>
        public int Number { get { return staff.ToList<LMeasure>().IndexOf(this) + 1; } }


        //////////// Overrides for add and remove operations //////////////
        /// <summary>
        /// Adds a LinkedList of MusicalSymbol objects to the end of the LMeasure.
        /// </summary>
        /// <param name="elem">The element to add.</param>
        internal void Add(List<MusicalSymbol> elems)
        {
            foreach (var elem in elems) base.AddLast(elem);
            trimExtras();            
        }

        /// <summary>
        /// Adds a MusicalSymbol object to the end of the LMeasure.
        /// </summary>
        /// <param name="elem">The element to add.</param>
        internal void Add(MusicalSymbol elem)
        {
            Add(new List<MusicalSymbol>() { elem });
        }

        /// <summary>
        /// Adds a LinkedList of musical elements immediately after the specified LinkedListNode. 
        /// </summary>
        /// <param name="node">The LinkedListNode that contains the target element.</param>
        /// <param name="elem">The elements to add.</param>
        internal void AddAfter(LinkedListNode<MusicalSymbol> node, List<MusicalSymbol> elems)
        {
            foreach (var elem in elems) base.AddAfter(node, elem);
            trimExtras();
        }

        /// <summary>
        /// Adds a musical element immediately after the specified LinkedListNode. 
        /// </summary>
        /// <param name="node">The LinkedListNode that contains the target element.</param>
        /// <param name="elem">The element to add.</param>
        internal new void AddAfter(LinkedListNode<MusicalSymbol> node, MusicalSymbol elem)
        {
            AddAfter(node, new List<MusicalSymbol>() { elem });
        }

        /// <summary>
        /// Adds the specified collection to the beginning of the LMeasure.
        /// </summary>
        /// <param name="elems">The items to add.</param>
        internal void AddFirst(List<MusicalSymbol> elems)
        {
            elems.Reverse();
            foreach (var item in elems) base.AddFirst(item);
            trimExtras();
        }
        /// <summary>
        /// Adds an element to the beginning of the LMeasure.
        /// </summary>
        /// <param name="elem">The element to add.</param>
        internal new void AddFirst(MusicalSymbol elem)
        {
            AddFirst(new List<MusicalSymbol>() { elem });
        }


        /// <summary>
        /// Deletes a MusicalSymbol object, such as a note or rest, from the LMeasure.
        /// </summary>
        /// <param name="elem">The item to delete.</param>
        /// <returns>true if deletion was successful. false otherwise.</returns>
        internal new bool Remove(MusicalSymbol elem)
        {
            bool success = base.Remove(elem);
            if (success)
            {
                bool filled = tryFillMeasure(this);
                trimExtras(); // in case of overfill, clean up the mess
            }
            return success;
        }


        /////////////// Private workhorse functions ////////////////
        /// <summary>
        /// If the current LMeasure is not full, tries to steal notes and rests from the beginning
        /// of the next LMeasure. It then recurses through the remainder of the staff until every
        /// measure except the last is full.
        /// </summary>
        /// <param name="m">The LMeasure to try to fill</param>
        /// <returns></returns>
        private bool tryFillMeasure(LMeasure m)
        {
            if (Beats >= capacity) return true;
            var next = m.Node.Next;
            while (Beats < capacity && next != null && next.Value.Beats > 0)
            {
                var elem = next.Value.First(e => e.GetType().IsSubclassOf(typeof(NoteOrRest)));
                m.AddLast(elem);
                next.Value.Remove(elem);
                if (next.Value.Beats == 0) tryFillMeasure(next.Value);
            }
            if (next != null) tryFillMeasure(next.Value);
            return m.Beats >= capacity;
        }

        /// <summary>
        /// Removes any extra beats or partial beats from the end of the LMeasure and inserts them at
        /// the beginning of the next LMeasure, recursively, until the end of the staff. If a note or rest
        /// runs over the bar line, it is split into two parts, and only the second part is moved.
        /// </summary>
        private void trimExtras()
        {
            var extras = new List<MusicalSymbol>();
            while (Beats > capacity)
            {
                var overage = Beats - capacity;
                var lastNoteOrRest = (NoteOrRest)this.Last(e => e.GetType().IsSubclassOf(typeof(NoteOrRest)));
                var lastDuration = lastNoteOrRest.Duration.ToDouble();
                base.Remove(lastNoteOrRest);
                if (lastDuration > overage)
                {
                    // break the last note or rest into two
                    NoteOrRest firstPart, secondPart;
                    if (lastNoteOrRest.GetType() == typeof(Note))
                    {
                        firstPart = new Note(((Note)lastNoteOrRest).Pitch, toRhythmicDuration(lastDuration - overage));
                        secondPart = new Note(((Note)lastNoteOrRest).Pitch, toRhythmicDuration(overage)); 
                    }
                    else
                    {
                        firstPart = new Rest(toRhythmicDuration(lastDuration - overage));
                        secondPart = new Rest(toRhythmicDuration(overage));
                    }
                    base.AddLast(firstPart);
                    extras.Add(secondPart);
                }
                else extras.Add(lastNoteOrRest);
            }

            arrangeElements();

            // This bit makes it recurse through subsequent measures
            if (Node == null || Node.Next == null)
            {
                if (extras.Count > 0)
                    staff.AddLast(new LMeasure(extras, staff, capacity));
            }
            else if (extras.Count > 0) Node.Next.Value.AddFirst(extras);
            else Node.Next.Value.trimExtras();   
        }

        /// <summary>
        /// Ensures that any clefs and signatures are at the beginning of the LMeasure, and that if full, there
        /// is exactly one bar line, and that it is at the end of the LMeasure.
        /// </summary>
        private void arrangeElements()
        {
            // Get rid of any null elements
            foreach (var v in this.Where(e => e == null))
                base.Remove(v);

            // Position any clefs and signatures
            //setSigs();
            var clef = this.FirstOrDefault(e => e.GetType() == typeof(Clef));
            var keySig = this.FirstOrDefault(e => e.GetType() == typeof(Key));
            var timeSig = this.FirstOrDefault(e => e.GetType() == typeof(TimeSignature));
            foreach (var elem in new MusicalSymbol[] { timeSig, keySig, clef})
            {
                //RemoveExtras(elem);
                if (elem != null)
                {
                    base.Remove(elem);                    
                    base.AddFirst(elem);
                }
            }

            // Set barlines
            while (this.Any(e => e.GetType() == typeof(Barline)))
                base.Remove(this.First(e => e.GetType() == typeof(Barline)));
            if (Beats == capacity) AddLast(new Barline());
            else if (Beats > capacity) throw new DataMisalignedException("There are too many beats in this measure.");
        }


        /// <summary>
        /// Converts a double value to a RhythmicDuration object. Supports note values down
        /// to 1/32nd with any number of dots.
        /// </summary>
        /// <param name="d">A double to convert.</param>
        /// <returns>A RhythmicDuration object whose value corresponds to d.</returns>
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

        /// <summary>
        /// Converts a RhythmicDuration object to a double. This function supports dotted values,
        /// unlike RhythmicDuration.ToDouble().
        /// </summary>
        /// <param name="rd"></param>
        /// <returns>A double that corresponds to the value of rd.</returns>
        private double DottedValue(RhythmicDuration rd)
        {
            double d = rd.WithoutDots.ToDouble();
            double dot = d / 2;
            for (int i = 0; i < rd.Dots; i++)
            {
                d += dot;
                dot = dot / 2;
            }
            return d;
        }
    }
}
