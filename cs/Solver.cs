using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sudoku
{

    class Solver
    {
        // fields
        private Stack<Grid> grids;
        private Grid curr;

        // properties
        private string puzzle;
        public string Puzzle
        {
            get { return puzzle; }
            set
            {
                puzzle = value;
                Solution = "";
                grids.Clear();
                curr = new Grid(puzzle);
            }
        }
        public string Solution { get; private set; }

        // ctors
        public Solver() { grids = new Stack<Grid>(); }
        public Solver(string puzzle)
            : this()
        {
            this.puzzle = puzzle;
            curr = new Grid(puzzle);
        }
        public Solver(Grid puzzle)
            : this()
        {
            curr = puzzle;
            this.puzzle = GridToString(puzzle);
        }

        // main method -- search for solution by iterating between (Eliminate > Guess > Eliminate > Guess > ...)
        public bool Solve()
        {
            // first eliminate/guess
            if (!Eliminate()) return false;
            if (IsSolved()) goto Done;
            Guess();

            // enter (search) loop
            while (grids.Count() > 0)
            {
                curr = grids.Pop();
                if (!Eliminate())
                    continue;
                if (IsSolved())
                    goto Done;
                else
                    Guess();
            }
            return false;

        Done:
            Solution = GridToString(curr);
            return true;
        }

        // helpers (private methods)
        private static string GridToString(Grid g)
        {
            StringBuilder sb = new StringBuilder();
            foreach (Entry e in g)
                sb.Append((e.Value.Count() == 1) ? new string(e.Value.ToArray()) : ".");
            return sb.ToString();
        }

        private bool IsValid()
        {
            foreach (Entry e in curr)
                if (!e.IsValid())
                    return false;
            return true;
        }

        private bool IsSolved()
        {
            foreach (Entry e in curr)
                if (!e.IsSolved())
                    return false;
            return true;
        }

        private void Guess()
        {
            Entry e = curr.NextGuess();
            int r = e.Row;
            int c = e.Column;
            foreach (char ch in e)
            {
                Grid g = new Grid(curr);
                g[r, c] = new Entry(r, c, ch);
                grids.Push(g);
            }
        }

        private bool Eliminate()
        {
            foreach (Entry e in curr)
            {
                if (e.IsSolved())
                {
                    char target = e.Value[0];
                    foreach (Entry s in curr.SameRow(e))
                        s.Remove(target);
                    foreach (Entry s in curr.SameColumn(e))
                        s.Remove(target);
                    foreach (Entry s in curr.SameBlock(e))
                        s.Remove(target);
                    if (!IsValid())
                        return false;
                }
            }
            return true;
        }

        // overrides
        public override string ToString() { return curr.ToString(); }
    }

}

