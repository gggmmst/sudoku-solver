using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sudoku
{

    class Entry : IEnumerable
    {
        // fields
        internal List<char> Value { get; set; }
        // properties
        public int Row { get; private set; }        // top row (0) ... bottom row (8)
        public int Column { get; private set; }     // leftmost col (0) ... rightmost col (8)
        public int Block
        {
            // 0 | 1 | 2
            //---+---+---
            // 3 | 4 | 5
            //---+---+---
            // 6 | 7 | 8
            get { return (Row / 3) * 3 + (Column / 3); }
        }
        // ctor
        internal Entry(int row, int column, char c)
        {
            Row = row;
            Column = column;
            Value = (c == '.') ? new List<char>("123456789") : new List<char>(c.ToString());
        }
        // copy-ctor
        internal Entry(Entry other)
        {
            Row = other.Row;
            Column = other.Column;
            Value = new List<char>(other.Value.Select(c => c).ToArray());
        }
        // methods
        public bool IsSolved() { return Value.Count() == 1; }
        public bool IsValid() { return Value.Count() > 0; }
        public bool Remove(char c) { return Value.Remove(c); }
        // impls and overrides
        IEnumerator IEnumerable.GetEnumerator() { return Value.GetEnumerator(); }
        public override string ToString() { return "[" + new string(Value.ToArray()) + "]"; }
    }

    class Grid : IEnumerable
    {
        // fields
        List<Entry> Entries;
        public Entry this[int row, int col]
        {
            get { return Entries[9 * row + col]; }
            set { Entries[9 * row + col] = value; }
        }

        // ctors
        public Grid() { Entries = new List<Entry>(); }
        public Grid(string puzzle)
            : this()
        {
            for (int r = 0; r < 9; ++r)
                for (int c = 0; c < 9; ++c)
                    Entries.Add(new Entry(r, c, puzzle[9 * r + c]));
        }

        // copy-ctor
        public Grid(Grid other) { Entries = other.Entries.Select(e => new Entry(e)).ToList(); }

        // iterators
        private delegate int EntryToRCB(Entry e);   // receives Entry and returns its Row, Column, or Block
        private IEnumerable<Entry> SameNeighbourhood(Entry e, EntryToRCB f)
        {
            var query = from entry in Entries
                        where f(entry) == f(e) && !object.ReferenceEquals(entry, e)
                        select entry;
            foreach (Entry entry in query)
                yield return entry;
        }
        public IEnumerable<Entry> SameRow(Entry e) { return SameNeighbourhood(e, s => s.Row); }
        public IEnumerable<Entry> SameColumn(Entry e) { return SameNeighbourhood(e, s => s.Column); }
        public IEnumerable<Entry> SameBlock(Entry e) { return SameNeighbourhood(e, s => s.Block); }

        //public IEnumerable<Entry> SameRow(Entry e)
        //{
        //    foreach (Entry v in Entries.Where(s => (s.Row == e.Row) && !object.ReferenceEquals(s, e)))
        //        yield return v;
        //}
        //
        //public IEnumerable<Entry> SameColumn(Entry e)
        //{
        //    foreach (Entry v in Entries.Where(s => (s.Column == e.Column) && !object.ReferenceEquals(s, e)))
        //        yield return v;
        //}
        //
        //public IEnumerable<Entry> SameBlock(Entry e)
        //{
        //    foreach (Entry v in Entries.Where(s => (s.Block == e.Block) && !object.ReferenceEquals(s, e)))
        //        yield return v;
        //}

        // methods
        public Entry NextGuess() { return Entries.Where(s => !s.IsSolved()).OrderBy(s => s.Value.Count()).First(); }

        // impls and overrides
        IEnumerator IEnumerable.GetEnumerator() { return Entries.GetEnumerator(); }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            int r = 0;
            foreach (Entry e in Entries)
            {
                if (r != e.Row)
                {
                    r = e.Row;
                    sb.Append('\n');
                }
                sb.Append(e.ToString());
            }
            return sb.ToString();
        }
    }

}
