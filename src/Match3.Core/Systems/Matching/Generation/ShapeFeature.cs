using System.Collections.Generic;
using Match3.Core.Models.Grid;

namespace Match3.Core.Systems.Matching.Generation;

public class ShapeFeature
{
    public List<HashSet<Position>> HLines { get; set; } = new();
    public List<HashSet<Position>> VLines { get; set; } = new();
    public int MinX;
    public int MaxX;
    public int MinY;
    public int MaxY;
}
