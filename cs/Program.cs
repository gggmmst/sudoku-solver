using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sudoku
{
    class ConsolePrinter
    {
        public static void PrintText(string puzzle) { Console.WriteLine(puzzle); }
        public static void PrintGrid(string puzzle)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < 9; ++i)
            {
                for (int j = 0; j < 9; ++j)
                {
                    sb.Append(" " + puzzle[9 * i + j]);
                    if (j == 2 || j == 5)
                        sb.Append(" |");
                }
                if (i == 2 || i == 5)
                    sb.Append("\n-------+-------+-------");
                sb.AppendLine();
            }
            Console.WriteLine(sb.ToString());
        }
    }

    class Program
    {
        static void Play(Solver s, string puzzle)
        {
            s.Puzzle = puzzle;
            if (s.Solve())
            {
                Console.WriteLine("Puzzle:");
                ConsolePrinter.PrintGrid(s.Puzzle);
                Console.WriteLine("Solution:");
                ConsolePrinter.PrintGrid(s.Solution);
            }
            else
            {
                Console.WriteLine("Invalid puzzle or no solution found.");
            }
            Console.WriteLine("=============================");
            Console.ReadKey();
        }

        static void Main(string[] args)
        {
            string p1 = "4.....8.5.3..........7......2.....6.....8.4......1.......6.3.7.5..2.....1.4......";
            string p2 = "52...6.........7.13...........4..8..6......5...........418.........3..2...87.....";
            string p3 = "6.....8.3.4.7.................5.4.7.3..2.....1.6.......2.....5.....8.6......1....";
            string hardest = "8..........36......7..9.2...5...7.......457.....1...3...1....68..85...1..9....4..";
            Solver s = new Solver();

            Play(s, p1);
            Play(s, p2);
            Play(s, p3);
            Play(s, hardest);
        }
    }
}

