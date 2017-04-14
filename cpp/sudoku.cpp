#include <algorithm>
#include <stack>
#include <string>
#include <vector>
#include <memory>
#include <iostream>

using namespace std;


class Entry {
// "Entry" represents each entry in a sudoku grid
// i.e. each sudoku grid is made up of 81 "Entry"

public:

    // each Entry could be located by is (row, col) coordinate
    int row;
    int col;
    // before a puzzle is solved, possible value(s) of an entry are {1,2,...,9}
    // when a puzzle is solved, each entry will have a value anywhere between 1 to 9
    vector<char> value;

    // ctor
    Entry(int r, int c, char v) : row(r), col(c) {
        value = ((v == '.') || (v == '*'))
                ? vector<char>({'1', '2', '3', '4', '5', '6', '7', '8', '9'})
                : vector<char>({v});
    }

    // copy ctor
    Entry(const Entry &other) : row(other.row), col(other.col), value(other.value) {}

    // assignment op
    Entry &operator=(const Entry &rhs) {
        Entry temp(rhs);
        swap(*this, temp);
        return *this;
    }

    ~Entry() = default;                       // default dtor

    bool remove(char target) {
        auto it = find(value.begin(), value.end(), target);
        if (it == value.end())      // target not found
            return false;
        value.erase(it);
        return true;
    }

    int block() const
    // each sudoku grid (which consists of 81 entries)
    // could be sub-divided into 9 sub-grids
    {
        // 0 | 1 | 2
        //---+---+---
        // 3 | 4 | 5
        //---+---+---
        // 6 | 7 | 8
        return (row / 3) * 3 + (col / 3);
    }

    // an entry is solved when its value vector only has one element left
    bool is_solved() const { return value.size() == 1; }

    bool is_valid() const { return value.size() > 0; }

    string to_string(bool bracket = true) const {
        string s;
        if (bracket)
            s += '[';
        for (auto const &v: value) { s += v; }
        if (bracket)
            s += ']';
        return s;
    }
};

using pEntry = shared_ptr<Entry>;


class Grid {
// a Grid has 81 entries

public:

    vector<pEntry> entries;

    // basic ctor
    Grid() : entries() { entries.reserve(81); }

    // ctor that takes a string
    explicit Grid(const string &puzzle) : Grid() {
        for (int r = 0; r < 9; ++r)
            for (int c = 0; c < 9; ++c) {
                pEntry p(new Entry(r, c, puzzle[9 * r + c]));
                entries.push_back(p);
            }
    }

    // copy ctor
    Grid(const Grid &other) : Grid()
    {
        for (auto const & p: other.entries)
        {
            pEntry q(new Entry(*p));        // deep copy
            entries.push_back(q);
        }
    }

    Grid &operator=(const Grid&) = delete;  // no assignment

    ~Grid() = default;                      // default dtor

    pEntry get_entry(int row, int col)
    {
        return entries[9 * row + col];
    }

    void set_entry(int row, int col, char ch)
    {
        auto p = get_entry(row, col);
        p->value = vector<char>({ch});
    }

    bool is_valid()
    {
        for (auto const &e: entries)
            if (!e->is_valid())
                return false;
        return true;
    }

    bool is_solved()
    {
        for (auto const &e: entries)
            if (!e->is_solved())
                return false;
        return true;
    }

    vector<pEntry> same_row(pEntry target) const
    // given an entry (target), returns all the other entries from the same row
    {
        vector<pEntry> res;
        copy_if(entries.begin(), entries.end(), back_inserter(res),
                [target](pEntry p) { return (target->row == p->row) && (target->col != p->col); });
        return res;
    }

    vector<pEntry> same_col(pEntry target) const
    // given an entry (target), returns all the other entries from the same column
    {
        vector<pEntry> res;
        copy_if(entries.begin(), entries.end(), back_inserter(res),
                [target](pEntry p) { return (target->row != p->row) && (target->col == p->col); });
        return res;
    }

    vector<pEntry> same_block(pEntry target) const
    // given an entry (target), returns all the other entries from the same block
    // each sudoku grid (with 81 entries) could be sub-divided into 9 blocks, see class Entry
    {
        vector<pEntry> res;
        copy_if(entries.begin(), entries.end(), back_inserter(res),
                [target](pEntry p) { return (target->block() == p->block() && !(target->row == p->row && target->col == p->col)); });
        return res;
    }

    pEntry next_guess() const
    // returns an entry which has not been solved, i.e. an entry which its value vector has more than one element
    // also, to speed up the search, returns an entry which is almost solved, i.e. min(entry.size())
    {
        pEntry p;
        int m = 9;
        for (auto const &e : entries)
        {
            if (!e->is_solved() && e->value.size() <= m)
            {
                p = e;
                m = e->value.size();
            }
        }
        return p;
    }

    string to_grid() const
    {
        string s;
        int r, c;
        for (auto const &e: entries)
        {
            s += e->is_solved() ? " " + e->to_string(false) : " *";
            r = e->row;
            c = e->col;
            if (c == 2 || c == 5)
                s += " |";
            if (c == 8) {
                s += "\n";
                if (r == 2 || r == 5)
                    s += "-------+-------+-------\n";
            }
        }
        return s;
    }

    string to_string() const
    {
        string s;
        for (auto const &e: entries)
        {
            s += e->to_string();
            if ((e->row != 8) && (e->col == 8))
                s += '\n';
        }
        return s;
    }

};

using pGrid = shared_ptr<Grid>;


class Solver {

    pGrid curr;
    stack<pGrid> grids;

public:

    explicit Solver(Grid *raw) : curr(pGrid(raw)), grids() { solve(); }
    explicit Solver(pGrid p) : curr(p), grids() { solve(); }

    Grid& solution() { return *curr; }

    bool solve()
    {
        // first eliminate/guess
        if (!eliminate())
            return false;
        if (curr->is_solved())
            return true;
        guess();

        // search loop
        while (grids.size() > 0)
        {
            curr = grids.top();
            grids.pop();
            if (!eliminate())
                continue;
            if (curr->is_solved())
                return true;
            else
                guess();
        }
        return false;
    }

    bool eliminate() {
        auto entries = curr->entries;
        for (auto const &e: entries)
        {
            if (e->is_solved())
            {
                char ch = e->value[0];
                auto row = curr->same_row(e);
                for (auto &r: row)
                    r->remove(ch);
                auto col = curr->same_col(e);
                for (auto &c: col)
                    c->remove(ch);
                auto block = curr->same_block(e);
                for (auto &b: block)
                    b->remove(ch);
                if (!curr->is_valid())
                    return false;
            }
        }
        return true;
    }

    void guess()
    {
        auto e = curr->next_guess();
        int r = e->row;
        int c = e->col;
        auto v = e->value;
        for (auto &ch: v)
        {
            pGrid g(new Grid(*curr));
            g->set_entry(r, c, ch);
            grids.push(g);
        }
    }

};


int solve_puzzles()
{
    vector<string> puzzles{
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
        // World's hardest sudoku
        // http://www.telegraph.co.uk/news/science/science-news/9359579/Worlds-hardest-sudoku-can-you-crack-it.html
        "8..........36......7..9.2...5...7.......457.....1...3...1....68..85...1..9....4..",
    };

    for (string const &puzzle: puzzles)
    {
        pGrid p(new Grid(puzzle));
        cout << p->to_grid() << endl;

        Solver solver(p);
        auto soln = solver.solution();
        cout << soln.to_grid() << endl;
        cout << "---" << endl;
    }
}


int main(int argc, char *argv[])
{
    solve_puzzles();
    return 0;
}

