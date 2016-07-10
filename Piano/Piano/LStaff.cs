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
        private MusicalSymbol selectedElement = null;


        public LStaff(Staff staff, Clef clef = null, Key key = null, TimeSignature timeSig = null, List<LMeasure> measures = null)
        {
            this.staff = staff;
            if (timeSig != null) measureDuration = timeSig.WholeNoteCapacity;
            if (measures != null) foreach (LMeasure measure in measures) AddLast(measure);
            else
            {
                List<MusicalSymbol> symbols = new List<MusicalSymbol>() { clef, key, timeSig };
                if (symbols.Any(e => e != null))
                {
                    LMeasure measure = new LMeasure(this, measureDuration);
                    foreach (var sym in symbols) if (sym != null) measure.Add(sym);
                    base.AddLast(measure);
                }
            }
        }

        public void Add(List<MusicalSymbol> elems)
        {
            Last.Value.Add(elems);
            updateStaff();
        }

        public void Add(MusicalSymbol elem)
        {
            Last.Value.Add(elem);
            updateStaff();
        }
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

        public void Remove(MusicalSymbol target)
        {
            LMeasure m = getMeasure(target);
            if (!m.Remove(target))
                throw new Exception("Could not delete " + target.ToString() + " from measure " + m.Number);
            updateStaff();
        }

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

        public void RestToNote(Rest r, Note n)
        {
            LMeasure m = getMeasure(r);
            var prev = m.Find(r).Previous;
            ((LinkedList<MusicalSymbol>)m).Remove(r);
            if (prev == null) m.AddFirst(n);
            else m.AddAfter(prev, n);
            updateStaff();
        }

        private LMeasure getMeasure(MusicalSymbol target) { return this.FirstOrDefault(m => m.Any(e => e == target)); }


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