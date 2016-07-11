namespace LData
{
    using Manufaktura.Controls.Model;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal class LStaff : LinkedList<LMeasure>
    {
        Staff staff;
        double measureDuration = 1;
        
        /// <summary>
        /// Constructor. Creates a new LStaff instance with the specified values.
        /// </summary>
        /// <param name="staff">Required. The Staff instance that this LStaff corresponds to.</param>
        /// <param name="clef">Optional. A clef object to add to the beginning of the staff.</param>
        /// <param name="key">Optional. A Key object to add as a key signature to the beginning of the staff.</param>
        /// <param name="timeSig">Optional. A TimeSignature object to add to the beginning of the staff. This time signature is
        /// applied by deafult to any measures created in the staff.</param>
        /// <param name="measures"></param>
        public LStaff(Staff staff, Clef clef = null, Key key = null, TimeSignature timeSig = null)
        {
            this.staff = staff;
            if (timeSig != null) measureDuration = timeSig.WholeNoteCapacity;
            List<MusicalSymbol> symbols = new List<MusicalSymbol>() { clef, key, timeSig };
            if (symbols.Any(e => e != null))
            {
                LMeasure measure = new LMeasure(this, measureDuration);
                foreach (var sym in symbols) if (sym != null) measure.Add(sym);
                base.AddLast(measure);
            }
        }

        /// <summary>
        /// Constructor. Creates a new LStaff instance with the supplied measures.
        /// </summary>
        /// <param name="staff">The Staff instance that this LStaff corresponds to.</param>
        /// <param name="measures">A collection of LMeasure objects representing measures on the staff.</param>
        public LStaff(Staff staff, List<LMeasure> measures)
        {
            this.staff = staff;
            foreach (LMeasure measure in measures)
            {
                AddLast(measure);
                measure.Staff = this;
            }
            MusicalSymbol sig = null;
            foreach (var m in this)
            {
                sig = m.FirstOrDefault(e => e.GetType() == typeof(TimeSignature));
                if (sig != null)
                {
                    measureDuration = ((TimeSignature)sig).WholeNoteCapacity;
                    break;
                }
            }
        }

        /// <summary>
        /// Adds the specified MusicalSymbol objects to the end of the staff, adding measures as needed.
        /// </summary>
        /// <param name="elems">The notes, rests, or other MusicalSymbol objects to add.</param>
        public void Add(List<MusicalSymbol> elems)
        {
            Last.Value.Add(elems);
            updateStaff();
        }

        /// <summary>
        /// Adds the specified MusicalSymbol object to the end of the staff, adding measures as needed.
        /// </summary>
        /// <param name="elems">The note, rest, or other MusicalSymbol object to add.</param>
        public void Add(MusicalSymbol elem)
        {
            Last.Value.Add(elem);
            updateStaff();
        }

        /// <summary>
        /// Adds the specified MuicalSymbol object to the staff in the position immediately after the selected note
        /// or rest, adjusting measures as needed.
        /// </summary>
        /// <param name="target">The note or rest that immediately precedes the new item.</param>
        /// <param name="itemToAdd">The note, rest, or other musical symbol to add.</param>
        public void AddAfter(MusicalSymbol target, MusicalSymbol itemToAdd)
        {
            LMeasure measure = getMeasure(target);
            if (measure == null) throw new ArgumentException("The selected element could not be found, or was in the wrong measure.");
            else
            {
                LinkedListNode<MusicalSymbol> node = measure.Find(target);
                if (node != null) measure.AddAfter(node, itemToAdd);
                else throw new ArgumentException("The selected element could not be found, or was in the wrong measure.");
            }
            updateStaff();
        }

        /// <summary>
        /// Deletes a MusicalSymbol object, such as a note or rest, from the staff.
        /// </summary>
        /// <param name="target">The item to delete.</param>
        public void Remove(MusicalSymbol target)
        {
            LMeasure m = getMeasure(target);
            if (!m.Remove(target))
                throw new Exception("Could not delete " + target.ToString() + " from measure " + m.Number);
            updateStaff();
        }

        /// <summary>
        /// Converts the selected note to a rest of the same duration.
        /// </summary>
        /// <param name="note">The selected note.</param>
        public void NoteToRest(Note note)
        {
            LMeasure m = getMeasure(note);
            var prev = m.Find(note).Previous;
            Rest r = new Rest(note.Duration);
            ((LinkedList<MusicalSymbol>)m).Remove(note);
            if (prev == null) m.AddFirst(r);
            else m.AddAfter(prev, r);
            updateStaff();
        }

        /// <summary>
        /// Deletes the selected rest and adds the specified note in its place.
        /// </summary>
        /// <param name="r">The selected rest.</param>
        /// <param name="n">The note to add.</param>
        public void RestToNote(Rest r, Note n)
        {
            LMeasure m = getMeasure(r);
            var prev = m.Find(r).Previous;
            ((LinkedList<MusicalSymbol>)m).Remove(r);
            if (prev == null) m.AddFirst(n);
            else m.AddAfter(prev, n);
            updateStaff();
        }

        /// <summary>
        /// Gets the LMeasure that corresponds to the measure that contains the specified element.
        /// </summary>
        /// <param name="target">The specified element.</param>
        /// <returns></returns>
        internal LMeasure getMeasure(MusicalSymbol target) { return this.FirstOrDefault(m => m.Any(e => e == target)); }

        /// <summary>
        /// Refreshes the content of the Manufactura.Controls.Staff object that the current staff corresponds to.
        /// </summary>
        private void updateStaff()
        {
            updateSysytemBreaks();
            staff.Elements.Clear();
            foreach (Measure m in staff.Measures) m.Elements.Clear();
            staff.Measures.Clear();

            foreach (LMeasure m in this)
            {
                foreach (var elem in m)
                {
                    staff.Elements.Add(elem);
                }
            }            
        }

        /// <summary>
        /// Updates the position of system breaks to land after every four measures.
        /// </summary>
        private void updateSysytemBreaks()
        {
            int i = 0;
            foreach (var m in this)
            {
                var ps = m.FirstOrDefault(e => e.GetType() == typeof(PrintSuggestion));
                if (ps != null) m.Remove(ps);
                if (i > 3 && i % 4 == 0)
                {
                    if (ps == null)
                    {
                        ps = new PrintSuggestion();
                        ((PrintSuggestion)ps).IsSystemBreak = true;
                    }
                    ((LinkedList<MusicalSymbol>) m).AddFirst(ps);
                }
                i++;
            }
        }
    }
}