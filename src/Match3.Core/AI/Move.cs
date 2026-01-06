namespace Match3.Core.AI;

public readonly struct Move
{
    public readonly Position From;
    public readonly Position To;

    public Move(Position from, Position to)
    {
        From = from;
        To = to;
    }

    public override string ToString() => $"[{From.X},{From.Y}] -> [{To.X},{To.Y}]";
}
