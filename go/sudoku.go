package main

import (
    "fmt"
    "bytes"
    "strconv"
    "sort"
)


// ===========================
// Entry
// ===========================

type Entry struct {
    Value []int
    Row int
    Column int
}

func NewEntry(s string, r, c int) *Entry {
    var v []int
    switch s {
    case ".":
        v = []int{1, 2, 3, 4, 5, 6, 7, 8, 9}
    default:
        i, _ := strconv.Atoi(s)
        v = []int{i}
    }
    return &Entry{Value: v, Row: r, Column: c}
}

func (e *Entry) Clone() Entry {
    v := append([]int(nil), e.Value...)
    return Entry{Value: v, Row: e.Row, Column: e.Column}
}

func (e *Entry) Block() int {
    return (e.Row / 3) * 3 + (e.Column / 3)
}

func (e *Entry) IsValid() bool {
    return len(e.Value) > 0
}

func (e *Entry) IsSolved() bool {
    return len(e.Value) == 1
}

func (e *Entry) index(target int) int {
    for i, v := range e.Value {
        if (target == v) {
            return i
        }
    }
    return -1
}

func (e *Entry) Remove(target int) bool {
    i := e.index(target)
    if (i == -1) {
        return false
    }
    e.Value = append(e.Value[:i], e.Value[i+1:]...)
    return true
}


// ===========================
// Grid
// ===========================

type Grid []*Entry

func NewGrid(puzzle string) *Grid {
    grid := Grid{}
    for r := 0; r < 9; r++ {
        for c:= 0; c < 9; c++ {
            e := 9 * r + c
            s := string(puzzle[e])
            grid = append(grid, NewEntry(s, r, c))
        }
    }
    return &grid
}

func GridToString(grid Grid) string {
    var buf bytes.Buffer
    var ch string
    for _, e := range grid {
        switch len(e.Value) {
        case 1:
            ch = fmt.Sprintf("%d", e.Value[0])
        default:
            ch = "."
        }
        buf.WriteString(ch)
    }
    return buf.String()
}

func (g *Grid) Clone() Grid {
    grid := Grid{}
    for _, e := range (*g) {
        clone := e.Clone()
        grid = append(grid, &clone)
    }
    return grid
}

func (g *Grid) SetValue(v []int, r, c int) {
    pos := 9 * r + c
    (*g)[pos].Value = v
}

func (g *Grid) IsSolved() bool {
    grid := *g
    for _, entry := range grid {
        if (!entry.IsSolved()) {
            return false
        }
    }
    return true
}

func (g *Grid) IsValid() bool {
    grid := *g
    for _, entry := range grid {
        if (!entry.IsValid()) {
            return false
        }
    }
    return true
}

func (g *Grid) sameNeighbourhood(e *Entry, attr func(*Entry) int) <-chan *Entry {
    ch := make(chan *Entry, 9)
    go func(e *Entry) {
        grid := *g
        for _, entry := range grid {
            if (attr(e) == attr(entry)) && (e != entry) {
                ch <- entry
            }
        }
        close(ch)
    }(e)
    return ch
}

func (g *Grid) SameRow(e *Entry) <-chan *Entry {
    return g.sameNeighbourhood(e, func(e *Entry) int { return e.Row })
}

func (g *Grid) SameColumn(e *Entry) <-chan *Entry {
    return g.sameNeighbourhood(e, func(e *Entry) int { return e.Column })
}

func (g *Grid) SameBlock(e *Entry) <-chan *Entry {
    return g.sameNeighbourhood(e, func(e *Entry) int { return e.Block() })
}

func GridTo9x9(puzzle string) string {
    var buf bytes.Buffer
    for r := 0; r < 9; r++ {
        for c := 0; c < 9; c++ {
            pos := 9 * r + c
            buf.WriteString(fmt.Sprintf(" %s", string(puzzle[pos])))
            if (c == 2 || c == 5) { buf.WriteString(" |") }
        }
        if (r == 2 || r == 5) { buf.WriteString("\n-------+-------+-------") }
        buf.WriteString("\n")
    }
    return buf.String()
}


// ===========================
// Stack
// ===========================

type Stack []Grid

func (s *Stack) Empty() bool {
    return len(*s) == 0
}

func (s *Stack) Size() int {
    return len(*s)
}

func (s *Stack) Pop() (Grid, bool) {
    n := len(*s)
    if (n == 0) {
        return nil, false
    }
    top := (*s)[n-1]
    *s = (*s)[:n-1]
    return top, true
}

func (s *Stack) Push(grid Grid) {
    *s = append(*s, grid)
}

func (s *Stack) Peek() Grid {
    n := len(*s)
    return (*s)[n-1]
}


// ===========================
// Solver
// ===========================

type Solver struct {
    Puzzle string
    Solution string
    curr *Grid
    grids Stack
}

//func (s *Solver) Solution() string {
//    return s.solution
//}

func (s *Solver) Solve(puzzle string) (string, bool) {
    // init
    s.Puzzle = puzzle
    s.Solution = ""
    s.curr = NewGrid(puzzle)
    s.grids = Stack{}
    // first eliminate/guess iteration
    if (!s.Eliminate()) { return "Invalid puzzle", false }
    if (s.curr.IsSolved()) { goto Done }
    s.Guess()
    // loop
    for (!s.grids.Empty()) {
        grid, _ := s.grids.Pop()
        s.curr = &grid
        if (!s.Eliminate()) { continue }
        if (s.curr.IsSolved()) {
            goto Done
        } else {
            s.Guess()
        }
    }
    return "No solution found", false
Done:
    soln := GridToString(*s.curr)
    s.Solution = soln
    return soln, true
}

func (s *Solver) Eliminate() bool {
    grid := *s.curr
    for _, e := range grid {
        if (e.IsSolved()) {
            target := e.Value[0]
            for q := range grid.SameRow(e)    { q.Remove(target) }
            for q := range grid.SameColumn(e) { q.Remove(target) }
            for q := range grid.SameBlock(e)  { q.Remove(target) }
            if (!grid.IsValid()) { return false }
        }
    }
    return true
}

type ByValueLength []*Entry
func (a ByValueLength) Len() int { return len(a) }
func (a ByValueLength) Swap(i, j int) { a[i], a[j] = a[j], a[i] }
func (a ByValueLength) Less(i, j int) bool { return len(a[i].Value) < len(a[j].Value) }

func (s *Solver) Guess() {
    grid := *s.curr
    var entries []*Entry
    for _, e := range grid {
        if (!e.IsSolved()) {
            entries = append(entries, e)
        }
    }
    sort.Sort(ByValueLength(entries))
    p := entries[0]
    for _, v := range p.Value {
        clone := grid.Clone()
        clone.SetValue([]int{v}, p.Row, p.Column)
        s.grids.Push(clone)
    }
}


// ===========================
// Main
// ===========================

func main() {
    puzzles := []string{
        "4.....8.5.3..........7......2.....6.....8.4......1.......6.3.7.5..2.....1.4......",
        "52...6.........7.13...........4..8..6......5...........418.........3..2...87.....",
        "6.....8.3.4.7.................5.4.7.3..2.....1.6.......2.....5.....8.6......1....",
        "48.3............71.2.......7.5....6....2..8.............1.76...3.....4......5....",
        "....14....3....2...7..........9...3.6.1.............8.2.....1.4....5.6.....7.8...",
        "......52..8.4......3...9...5.1...6..2..7........3.....6...1..........7.4.......3.",
        "6.2.5.........3.4..........43...8....1....2........7..5..27...........81...6.....",
        ".524.........7.1..............8.2...3.....6...9.5.....1.6.3...........897........",
        "6.2.5.........4.3..........43...8....1....2........7..5..27...........81...6.....",
        ".923.........8.1...........1.7.4...........658.........6.5.2...4.....7.....9.....",
    }
    hardest := "8..........36......7..9.2...5...7.......457.....1...3...1....68..85...1..9....4.."
    puzzles = append(puzzles, hardest)

    solver := Solver{}
    for i, puzzle := range puzzles {
        fmt.Printf("Puzzle #%d:\n", i+1)
        fmt.Println(GridTo9x9(puzzle))
        solver.Solve(puzzle)
        fmt.Println("Solution:")
        fmt.Println(GridTo9x9(solver.Solution))
        fmt.Println("=======================")
    }
}

