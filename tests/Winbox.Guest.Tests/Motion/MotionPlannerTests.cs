using Winbox.Guest.Motion;
using Winbox.Protocol;

namespace Winbox.Guest.Tests.Motion;

public class MotionPlannerTests
{
    // Mirrors MotionPlanner's private tuning, so changing the feel of a move is a deliberate edit in both places.
    private const int MinDurationMs = 180, MaxDurationMs = 650;
    private const double ArcFraction = 0.08;

    // Rounding each coordinate to a whole pixel can shift a point by up to half a pixel on both axes.
    private static readonly double RoundingSlack = Math.Sqrt(0.5);

    [Theory]
    [InlineData(0, 0, 1919, 1079)]
    [InlineData(800, 600, 10, 20)]
    [InlineData(-1920, 300, 640, -200)]
    [InlineData(5, 5, 6, 5)]
    public void PlanPath_EndsExactlyAtTarget(int fromX, int fromY, int toX, int toY)
    {
        var target = new Point(toX, toY);

        var path = Planner().PlanPath(new Point(fromX, fromY), target);

        Assert.Equal(target, path.Last());
    }

    [Theory]
    [InlineData(0, 0, 1919, 1079)]
    [InlineData(800, 600, 10, 20)]
    [InlineData(100, 100, 200, 100)]
    public void PlanPath_FirstPoint_IsNearStart(int fromX, int fromY, int toX, int toY)
    {
        var from = new Point(fromX, fromY);
        var to = new Point(toX, toY);

        var first = Planner().PlanPath(from, to).First();

        Assert.InRange(Distance(from, first), 0, Distance(from, to) * 0.02 + RoundingSlack);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(300)]
    [InlineData(1500)]
    [InlineData(100_000)]
    public void PlanPath_StepCount_StaysWithinDurationBounds(int distance)
    {
        var path = Planner().PlanPath(new Point(0, 0), new Point(distance, 0));

        // One point per frame, plus at most one more for landing on the target.
        Assert.InRange(path.Count, MinDurationMs / MotionPlanner.FrameMs, MaxDurationMs / MotionPlanner.FrameMs + 1);
    }

    [Theory]
    [InlineData(10, 200)]
    [InlineData(200, 1000)]
    [InlineData(1000, 1300)]
    public void PlanPath_LongerMove_TakesMoreSteps(int shorter, int longer)
    {
        var planner = Planner();

        var shortPath = planner.PlanPath(new Point(0, 0), new Point(shorter, 0));
        var longPath = planner.PlanPath(new Point(0, 0), new Point(longer, 0));

        Assert.True(longPath.Count > shortPath.Count, $"{longPath.Count} steps should exceed {shortPath.Count}");
    }

    [Fact]
    public void PlanPath_ZeroDistance_StaysOnTarget()
    {
        var target = new Point(640, 360);

        var path = Planner().PlanPath(target, target);

        Assert.All(path, point => Assert.Equal(target, point));
    }

    [Theory]
    [InlineData(100, 900)]
    [InlineData(900, 100)]
    public void PlanPath_HorizontalMove_NeverBacktracksAlongTheMoveAxis(int fromX, int toX)
    {
        var direction = Math.Sign(toX - fromX);

        var path = Planner().PlanPath(new Point(fromX, 500), new Point(toX, 500));

        var progress = path.Select(point => (point.X - fromX) * direction).ToList();
        Assert.Equal(progress.OrderBy(x => x), progress);
    }

    [Fact]
    public void PlanPath_HorizontalMove_IsSlowerAtBothEndsThanInTheMiddle()
    {
        var path = Planner().PlanPath(new Point(0, 0), new Point(1000, 0));

        var stepLengths = path.Zip(path.Skip(1), (a, b) => b.X - a.X).ToList();

        var middle = stepLengths[stepLengths.Count / 2];
        Assert.True(stepLengths.First() < middle && stepLengths.Last() < middle, string.Join(",", stepLengths));
    }

    [Theory]
    [InlineData(1, 0, 0, 1000, 0)]
    [InlineData(2, 0, 0, 1000, 0)]
    [InlineData(3, 100, 800, 1500, 50)]
    [InlineData(4, 1500, 50, 100, 800)]
    [InlineData(5, 300, 300, 330, 260)]
    public void PlanPath_Bow_StaysWithinArcFractionOfDistance(int seed, int fromX, int fromY, int toX, int toY)
    {
        var from = new Point(fromX, fromY);
        var to = new Point(toX, toY);

        var path = Planner(seed).PlanPath(from, to);

        var limit = Distance(from, to) * ArcFraction + RoundingSlack;
        Assert.All(path, point => Assert.InRange(OffLineDistance(from, to, point), 0, limit));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void PlanPath_LongMove_BowsVisiblyOffTheStraightLine(int seed)
    {
        var from = new Point(0, 0);
        var to = new Point(1000, 0);

        var path = Planner(seed).PlanPath(from, to);

        var widest = path.Max(point => OffLineDistance(from, to, point));
        Assert.True(widest > Distance(from, to) * ArcFraction / 2, $"widest offset was {widest}px");
    }

    private static MotionPlanner Planner(int seed = 1) => new(new Random(seed));

    private static double Distance(Point a, Point b) => Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));

    /// <summary>Perpendicular distance from <paramref name="point"/> to the line through both endpoints.</summary>
    private static double OffLineDistance(Point from, Point to, Point point)
    {
        double dx = to.X - from.X, dy = to.Y - from.Y;
        var cross = dx * (point.Y - from.Y) - dy * (point.X - from.X);
        return Math.Abs(cross) / Math.Sqrt(dx * dx + dy * dy);
    }
}
