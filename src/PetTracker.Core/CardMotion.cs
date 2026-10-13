namespace PetTracker.Core;

// Presentation-only motion. It never owns or changes pet progress or visibility preferences.
public sealed class CardMotion(float stride, double duration = .26)
{
    public sealed class Row(int id, float y, double now)
    {
        public int Id { get; } = id;
        public float Y { get; internal set; } = y;
        public float X { get; internal set; } = 28;
        public float Alpha { get; internal set; }
        public bool Removing { get; internal set; }
        internal float FromY = y, ToY = y, FromX = 28, ToX;
        internal float FromAlpha, ToAlpha = 1;
        internal double Start = now;
        internal double MoveStart = now;
    }
    private readonly Dictionary<int, Row> rows = [];
    public IReadOnlyCollection<Row> Rows => rows.Values;
    public bool IsAnimating=>rows.Values.Any(r=>r.Removing||Math.Abs(r.Y-r.ToY)>.01||Math.Abs(r.X-r.ToX)>.01||Math.Abs(r.Alpha-r.ToAlpha)>.01);
    private static float Ease(double value) => 1 - MathF.Pow(1 - (float)Math.Clamp(value, 0, 1), 3);
    public void Sync(IEnumerable<int> orderedIds, double now)
    {
        Tick(now);
        var ids = orderedIds.Distinct().ToArray(); var wanted = ids.ToHashSet();
        foreach (var row in rows.Values.Where(r => !r.Removing && !wanted.Contains(r.Id)))
        {
            row.Removing = true; row.FromX = row.X; row.ToX = 28;
            row.FromAlpha = row.Alpha; row.ToAlpha = 0; row.Start = now;
        }
        for (var i = 0; i < ids.Length; i++)
        {
            var y = i * stride;
            if (!rows.TryGetValue(ids[i], out var row)) rows.Add(ids[i], row = new(ids[i], y, now));
            if (row.Removing)
            {
                row.Removing = false; row.FromX = row.X; row.ToX = 0;
                row.FromAlpha = row.Alpha; row.ToAlpha = 1; row.Start = now;
            }
            if (Math.Abs(row.ToY - y) > .01)
            { row.FromY = row.Y; row.ToY = y; row.MoveStart = now; }
        }
    }
    public void Tick(double now)
    {
        foreach (var row in rows.Values.ToArray())
        {
            var t = Ease((now - row.Start) / duration); var move = Ease((now - row.MoveStart) / duration);
            row.X = row.FromX + (row.ToX - row.FromX) * t;
            row.Alpha = row.FromAlpha + (row.ToAlpha - row.FromAlpha) * t;
            row.Y = row.FromY + (row.ToY - row.FromY) * move;
            if (row.Removing && now - row.Start >= duration) rows.Remove(row.Id);
        }
    }
}
