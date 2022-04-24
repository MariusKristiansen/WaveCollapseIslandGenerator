using System.Text;

namespace IslandGenerator;

public class Generator
{
    public int XDim { get; set; }
    public int YDim { get; set; }
    internal Random Rng { get; set; }
    public SuperposedCell[,] Island { get; set; }

    public static string GenerateIsland(int xDim = 20, int yDim = 20, int? seed = null)
    {
        var gen = new Generator(xDim, yDim, seed);
        gen.Generate();
        return gen.GetLayout();
    }

    private Generator(int xDim, int yDim, int? seed)
    {
        Rng = seed is null ? new() : new((int)seed);
        XDim = xDim;
        YDim = yDim;
        Island = new SuperposedCell[XDim, YDim];
        InitializeIslandArray();
    }
    
    public int FindLowestEntropy()
    {
        int entropy = 5;
        foreach (var cell in Island)
        {
            if (cell.IsCollapsed) continue;
            if (cell.Entropy < entropy)
            {
                entropy = cell.Entropy;
            }
        }
        return entropy;
    }

    private void CollapseRandomCell(List<SuperposedCell> cells)
    {
        int index = Rng.Next(cells.Count);
        if (cells.Any()) cells[index].Collapse();
    }

    public List<SuperposedCell> GetCellsWithEntropy(int entropy)
    {
        List<SuperposedCell> cells = new();
        foreach (var cell in Island)
        {
            if (cell.Entropy == entropy)
            {
                cells.Add(cell);
            }
        }
        return cells;
    }

    public void Generate()
    {
        while (!OneCycle());
    }

    public bool OneCycle()
    {
        int entropy = FindLowestEntropy();
        var cells = GetCellsWithEntropy(entropy);
        CollapseRandomCell(cells);
        ReduceEntropy();
        return Done();
    }

    private bool Done()
    {
        foreach (var cell in Island) if (!cell.IsCollapsed) return false;
        return true;
    }

    private void ResetReducedFlags()
    {
        foreach (var cell in Island)
        {
            cell.Reduced = false;
        }
    }
    
    private bool CollapseRandomCell()
    {
        int xRand = Rng.Next(XDim);
        int yRand = Rng.Next(YDim);
        return CollapseCell(Island[xRand, yRand]);
    }

    public void CollapseRandomCells(int cells = 1)
    {
        for (int i = 0; i < cells; i++)
        {
            bool collapsed;
            do collapsed = CollapseRandomCell();
            while (!collapsed);
        }
    }

    private static bool CollapseCell(SuperposedCell cell) {
        if (cell.IsCollapsed) return false;
        cell.IsCollapsed = true;
        cell.Collapse();
        return true;
    }
    
    public void ReduceEntropy()
    {
        foreach (var cell in Island) cell.ReduceNeighbors();
        ResetReducedFlags();
    }

    private void InitializeIslandArray()
    {
        for (int x = 0; x < XDim; x++)
        {
            for (int y = 0; y < YDim; y++)
            {
                Island[x, y] = new SuperposedCell(x, y, this);
            }
        }
    }

    public string GetLayout()
    {
        string output = "";
        for (int x = 0; x < XDim; x++)
        {
            for (int y = 0; y < YDim; y++)
            {
                output += Island[x, y].CellType.MyType.ToString()[0];
            }
            output += "\n";
        }
        return output;
    }

    public override string ToString()
    {
        string output = "";
        for (int x = 0; x < XDim; x++)
        {
            output += "|";
            for (int y = 0; y < YDim; y++)
            {
                output += Island[x, y].ToString();
            }
            output += "\n";
        }
        return output;
    }
}

public class SuperposedCell
{
    public int XPos { get; set; }
    public int YPos { get; set; }
    public bool IsCollapsed { get; set; } = false;
    public List<Island.Cell> Superpositions { get; set; }
    public Island.Cell CellType { get; set; }
    public Generator Generator { get; set; }
    public bool Reduced { get; set; } = false;

    public void Collapse()
    {
        CellType = Superpositions[Generator.Rng.Next(Superpositions.Count)];
        Superpositions.Clear();
        Superpositions.Add(CellType);
        IsCollapsed = true;
    }

    List<Island.Type> ValidConnectors => Neighbours.Aggregate(new List<Island.Type>(), (acc, neighbour) =>
    {
        if (neighbour.IsCollapsed) return acc;
        acc.AddRange(neighbour.Superpositions.Select(x => x.Connectors).Aggregate(new List<Island.Type>(), (acc2, connector) =>
        {
            acc2.AddRange(connector);
            return acc2;
        }));
        return acc;
    }).Distinct().ToList();

    public SuperposedCell(int xPosition, int yPosition, Generator generator)
    {
        XPos = xPosition;
        YPos = yPosition;
        Superpositions = Island.StartValue;
        Generator = generator;
    }
    
    public void Reset()
    {
        Reduced = false;
    }

    public List<SuperposedCell> Neighbours
    {
        get
        {
            List<SuperposedCell> neighbours = new List<SuperposedCell>();
            for (int x = -1; x <= 1; x++)
            {
                if (x == 0) continue;
                int xPos = XPos + x;
                if (xPos < 0 || xPos >= Generator.XDim) continue;
                neighbours.Add(Generator.Island[xPos, YPos]);
            }

            for (int y = -1; y <= 1; y++)
            {
                if (y == 0) continue;
                int yPos = YPos + y;
                if (yPos < 0 || yPos >= Generator.YDim) continue;
                neighbours.Add(Generator.Island[XPos, yPos]);
            }

            return neighbours;
        }
    }

    public int Entropy => Superpositions.Count;

    private void ControlConnectors(SuperposedCell neighbour, List<Island.Type> connectors)
    {
        foreach (var connector in (Island.Type[])Enum.GetValues(typeof(Island.Type)))
        {
            if (connectors.Contains(connector))
            {
                continue;
            }
            else
            {
                neighbour.Superpositions.TryRemove(connector);
            }
        }
        
    }

    public void ReduceNeighbors()
    {
        if (!IsCollapsed) return;
        foreach (var neighbour in Neighbours)
        {
            if (neighbour.IsCollapsed) continue;

            if (neighbour.YPos == YPos + 1 && neighbour.XPos == XPos)
            {
                ControlConnectors(neighbour, CellType.NorthConnectors);
            } 
            else if (neighbour.YPos == YPos - 1 && neighbour.XPos == XPos)
            {
                ControlConnectors(neighbour, CellType.SouthConnectors);
            }
            else if (neighbour.YPos == YPos && neighbour.XPos == XPos + 1)
            {
                ControlConnectors(neighbour, CellType.EastConnectors);
            }
            else if (neighbour.YPos == YPos && neighbour.XPos == XPos - 1)
            {
                ControlConnectors(neighbour, CellType.WestConnectors);
            } 
            else
            {
                throw new InvalidDataException();
            }
        }
        Reduced = true;
    }

    public override string ToString()
    {
        StringBuilder sb = new();
        for (int i = 0; i < 5; i++)
        {
            if (Superpositions.Count > i)
            {
                sb.Append(Superpositions[i].MyType.ToString()[0]);
            }
            else
            {
                sb.Append(' ');
            }
            
        }
        return sb.ToString() + "|";
    }
}



public static class Island
{

    public static bool TryRemove(this List<Island.Cell> cells, Island.Type type) {
        var index = cells.FindIndex(x => x.MyType == type);
        if (index == -1) return false;
        cells.RemoveAt(index);
        return true;
    }
    
    public enum Type
    {
        Mountains,
        Ocean,
        Savannah,
        Jungle,
        Desert
    }

    public static List<Cell> StartValue => new List<Cell>() { new Ocean(), new Savannah(), new Mountains(), new Desert(), new Jungle() };

    public abstract class Cell
    {
        public abstract Island.Type MyType { get; }
        public List<Island.Type> NorthConnectors { get; set; }
        public List<Island.Type> SouthConnectors { get; set; }
        public List<Island.Type> EastConnectors { get; set; }
        public List<Island.Type> WestConnectors { get; set; }

        public List<Island.Type> Connectors => new List<Type>().Concat(NorthConnectors).Concat(SouthConnectors).Concat(EastConnectors).Concat(WestConnectors).Distinct().ToList();
        public List<Type>[] ConnectorArray => new List<Type>[] { NorthConnectors, SouthConnectors, EastConnectors, WestConnectors };
        public Cell()
        {
            NorthConnectors = new List<Type>() { MyType };
            SouthConnectors = new List<Type>() { MyType };
            EastConnectors = new List<Type>() { MyType };
            WestConnectors = new List<Type>() { MyType };
        }
    }

    public class Savannah : Cell
    {
        public Savannah()
        {
            NorthConnectors.Add(Type.Jungle);
            NorthConnectors.Add(Type.Mountains);
            NorthConnectors.Add(Type.Desert);

            SouthConnectors.Add(Type.Jungle);
            SouthConnectors.Add(Type.Mountains);
            SouthConnectors.Add(Type.Desert);

            EastConnectors.Add(Type.Jungle);
            EastConnectors.Add(Type.Mountains);
            EastConnectors.Add(Type.Desert);

            WestConnectors.Add(Type.Jungle);
            WestConnectors.Add(Type.Mountains);
            WestConnectors.Add(Type.Desert);
        }

        public override Type MyType => Type.Savannah;
    }

    public class Jungle : Cell
    {
        public Jungle()
        {
            NorthConnectors.Add(Type.Savannah);
            NorthConnectors.Add(Type.Ocean);

            SouthConnectors.Add(Type.Savannah);
            SouthConnectors.Add(Type.Ocean);

            EastConnectors.Add(Type.Savannah);
            EastConnectors.Add(Type.Ocean);

            WestConnectors.Add(Type.Savannah);
            WestConnectors.Add(Type.Ocean);

        }

        public override Type MyType => Type.Jungle;
    }

    public class Mountains : Cell
    {
        public Mountains()
        {
            NorthConnectors.Add(Type.Savannah);

            SouthConnectors.Add(Type.Savannah);

            EastConnectors.Add(Type.Savannah);

            WestConnectors.Add(Type.Savannah);
        }

        public override Type MyType => Type.Mountains;
    }

    public class Ocean : Cell
    {
        public Ocean()
        {
            NorthConnectors.Add(Type.Savannah);
            NorthConnectors.Add(Type.Jungle);

            SouthConnectors.Add(Type.Savannah);
            SouthConnectors.Add(Type.Jungle);

            EastConnectors.Add(Type.Savannah);
            EastConnectors.Add(Type.Jungle);

            WestConnectors.Add(Type.Savannah);
            WestConnectors.Add(Type.Jungle);
        }

        public override Type MyType => Type.Ocean;
    }

    public class Desert : Cell
    {
        public Desert()
        {
            NorthConnectors.Add(Type.Savannah);
            NorthConnectors.Add(Type.Jungle);

            SouthConnectors.Add(Type.Savannah);
            SouthConnectors.Add(Type.Jungle);

            EastConnectors.Add(Type.Savannah);
            EastConnectors.Add(Type.Jungle);

            WestConnectors.Add(Type.Savannah);
            WestConnectors.Add(Type.Jungle);
        }

        public override Type MyType => Type.Desert;
    }
}
