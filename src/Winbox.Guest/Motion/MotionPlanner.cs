using System;
using System.Collections.Generic;
using Winbox.Protocol;

namespace Winbox.Guest.Motion;

/// <summary>
/// Plans a cursor path that looks hand-driven: eased in and out, bowed slightly off the straight line,
/// and slower for longer distances.
/// </summary>
internal sealed class MotionPlanner(Random random)
{
    private const double MinDurationMs = 180, MaxDurationMs = 650, MsPerPixel = 0.35;
    private const double ArcFraction = 0.08;
    public const int FrameMs = 8;

    public IReadOnlyList<Point> PlanPath(Point from, Point to)
    {
        var distance = Distance(from, to);
        var steps = (int)(DurationMs(distance) / FrameMs);
        var bow = distance * ArcFraction * (random.Next(2) == 0 ? -1 : 1);
        var path = new List<Point>(steps + 1);
        for (var step = 1; step < steps; step++)
        {
            path.Add(PointAlong(from, to, (double)step / steps, bow));
        }
        path.Add(to);
        return path;
    }

    private static double DurationMs(double distance) =>
        Math.Min(MaxDurationMs, MinDurationMs + distance * MsPerPixel);

    private static Point PointAlong(Point from, Point to, double t, double bow)
    {
        var eased = t * t * (3 - 2 * t);
        var dx = to.X - from.X;
        var dy = to.Y - from.Y;
        var length = Math.Max(1, Math.Sqrt(dx * dx + dy * dy));
        // Offset along the perpendicular, peaking mid-path, gives the gentle arc of a real wrist movement.
        var offset = Math.Sin(Math.PI * t) * bow;
        var x = from.X + dx * eased - dy / length * offset;
        var y = from.Y + dy * eased + dx / length * offset;
        return new Point((int)Math.Round(x), (int)Math.Round(y));
    }

    private static double Distance(Point a, Point b) =>
        Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));
}
