using System;
using System.Collections.Generic;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>
    /// How generously the movement model is read. <see cref="Safe"/> uses jumps a new player can make comfortably (3 cells up,
    /// 4 across), to prove a route exists. <see cref="Max"/> uses the physical limits from GDD 4 plus margin (jump 4.5 up, jump +
    /// dash 10 across, Wind double jump 7.2 up), to prove a gate cannot be passed without its Aura.
    /// </summary>
    public enum ReachMode
    {
        Safe,
        Max,
    }

    /// <summary>
    /// Grid reachability of one room: a breadth-first search over "standing cells" (two free cells above a support). It ignores
    /// timing hazards (pistons, steam, stalactites) and enemies, treats spikes, acid and kill zones as forbidden cells, and opens
    /// gates according to <see cref="LevelAbilities"/>. Wall jumps and dashes are not modelled in <see cref="ReachMode.Safe"/>;
    /// <see cref="ReachMode.Max"/> covers them through the larger jump limits (smooth walls are solid and never grip).
    /// </summary>
    public static class LevelReachability
    {
        sealed class Params
        {
            public int MaxUp, MaxApex, Span;
            public Func<int, int> Limit;
        }

        public static HashSet<Vector2Int> Reach(RoomFile room, Vector2Int start, LevelAbilities ab, ReachMode mode)
        {
            var seen = new HashSet<Vector2Int> { start };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);
            var found = new List<Vector2Int>();
            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                found.Clear();
                Neighbours(room, cur.x, cur.y, ab, mode, found);
                foreach (var n in found)
                    if (seen.Add(n)) queue.Enqueue(n);
            }
            return seen;
        }

        /// <summary>True when a reachable standing cell is on the cell or directly above it (a chest or altar stands on its floor cell).</summary>
        public static bool ReachesCell(HashSet<Vector2Int> states, Vector2Int cell) =>
            states.Contains(cell) || states.Contains(cell + Vector2Int.down);

        public static bool ReachesAny(HashSet<Vector2Int> states, IEnumerable<Vector2Int> cells)
        {
            foreach (var c in cells)
                if (ReachesCell(states, c)) return true;
            return false;
        }

        public static bool IsStandable(RoomFile r, int x, int y, LevelAbilities ab)
        {
            if (x < 0 || y < 0 || x >= r.Width || y >= r.Height) return false;
            if (BodyBlocked(r, x, y, ab) || BodyBlocked(r, x, y + 1, ab)) return false;
            char c = r.At(x, y);
            if (c == 'Y' && Has(ab, LevelAbilities.Wind)) return true;
            if (c == '~' && Has(ab, LevelAbilities.Water)) return true;
            return HasSupport(r, x, y - 1, ab);
        }

        static bool Has(LevelAbilities ab, LevelAbilities flag) => (ab & flag) != 0;

        static bool BodyBlocked(RoomFile r, int x, int y, LevelAbilities ab)
        {
            switch (r.At(x, y))
            {
                case '#': case 'W': case 'c': case '^': case 'K': case 'a': return true;
                case 'k': case 'j': return !Has(ab, LevelAbilities.ShortcutOpen);
                case 'Z': case 'T': return !Has(ab, LevelAbilities.Fire);
                case 'G': return !Has(ab, LevelAbilities.Seals);
                case 'f': return !Has(ab, LevelAbilities.Water);
                default: return false;
            }
        }

        static bool HasSupport(RoomFile r, int x, int y, LevelAbilities ab)
        {
            switch (r.At(x, y))
            {
                case '#': case 'W': case '=': case 'c': return true;
                case 'k': case 'j': return !Has(ab, LevelAbilities.ShortcutOpen);
                case 'Z': case 'T': return !Has(ab, LevelAbilities.Fire);
                case 'G': return !Has(ab, LevelAbilities.Seals);
                default: return false;
            }
        }

        static Params ParamsFor(LevelAbilities ab, bool slow, ReachMode mode)
        {
            bool wind = Has(ab, LevelAbilities.Wind);
            if (slow) return new Params { MaxUp = 1, MaxApex = 2, Span = 4, Limit = dy => dy <= 1 ? 4 : 0 };
            if (mode == ReachMode.Safe)
            {
                if (!wind) return new Params { MaxUp = 3, MaxApex = 4, Span = 4, Limit = dy => dy <= 2 ? 4 : (dy == 3 ? 3 : 0) };
                return new Params { MaxUp = 6, MaxApex = 6, Span = 4, Limit = dy => dy <= 2 ? 4 : (dy <= 5 ? 3 : 2) };
            }
            if (!wind) return new Params { MaxUp = 4, MaxApex = 5, Span = 10, Limit = dy => dy <= 0 ? 10 : (dy <= 2 ? 9 : 7) };
            return new Params { MaxUp = 7, MaxApex = 8, Span = 16, Limit = dy => dy <= 0 ? 16 : (dy <= 3 ? 12 : 8) };
        }

        static void Neighbours(RoomFile r, int x, int y, LevelAbilities ab, ReachMode mode, List<Vector2Int> output)
        {
            char c = r.At(x, y);
            bool slow = c == '~' && !Has(ab, LevelAbilities.Water);
            var p = ParamsFor(ab, slow, mode);
            for (int dx = -p.Span; dx <= p.Span; dx++)
            {
                for (int y2 = Math.Max(0, y - 40); y2 <= y + p.MaxUp; y2++)
                {
                    int dy = y2 - y;
                    if ((dx == 0 && dy == 0) || Math.Abs(dx) > p.Limit(dy)) continue;
                    if (!IsStandable(r, x + dx, y2, ab)) continue;
                    for (int ha = y; ha <= y + p.MaxApex; ha++)
                    {
                        if (!PathFree(r, x, y, x + dx, y2, ha, ab)) continue;
                        output.Add(new Vector2Int(x + dx, y2));
                        break;
                    }
                }
            }
            if (c == 'Y' && Has(ab, LevelAbilities.Wind))
            {
                foreach (int yy in new[] { y - 1, y + 1 })
                    if (r.At(x, yy) == 'Y' && IsStandable(r, x, yy, ab)) output.Add(new Vector2Int(x, yy));
            }
            if (c == '~' && Has(ab, LevelAbilities.Water)) SwimNeighbours(r, x, y, ab, output);
        }

        static void SwimNeighbours(RoomFile r, int x, int y, LevelAbilities ab, List<Vector2Int> output)
        {
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    if ((dx != 0 || dy != 0) && r.At(x + dx, y + dy) == '~' && IsStandable(r, x + dx, y + dy, ab))
                        output.Add(new Vector2Int(x + dx, y + dy));
        }

        /// <summary>Up the start column to the apex row, across at the apex, then along the target column to the landing; every body cell must be free.</summary>
        static bool PathFree(RoomFile r, int x, int y, int x2, int y2, int apex, LevelAbilities ab)
        {
            for (int yy = y; yy <= apex + 1; yy++)
                if (BodyBlocked(r, x, yy, ab)) return false;
            for (int xx = Math.Min(x, x2); xx <= Math.Max(x, x2); xx++)
                if (BodyBlocked(r, xx, apex, ab) || BodyBlocked(r, xx, apex + 1, ab)) return false;
            for (int yy = Math.Min(apex, y2); yy <= Math.Max(apex, y2) + 1; yy++)
                if (BodyBlocked(r, x2, yy, ab)) return false;
            return true;
        }
    }
}
