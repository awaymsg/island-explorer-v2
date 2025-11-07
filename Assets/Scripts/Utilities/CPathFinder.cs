using System.Collections.Generic;
using UnityEngine;

public class CPathFinder
{
    private STerrainTile[,] m_TileGrid;
    bool m_Diagonals;
    int m_MaxX, m_MaxY;
    bool m_bHasFog = true;

    // Constructor takes in tilegrid and allow diagonal
    public CPathFinder(bool diag, STerrainTile[,] tileGrid, bool bHasFog)
    {
        m_TileGrid = tileGrid;
        m_Diagonals = diag;

        m_MaxX = m_TileGrid.GetLength(0);
        m_MaxY = m_TileGrid.GetLength(1);
        m_bHasFog = bHasFog;
    }

    public void SetHasFog(bool bHasFog)
    {
        m_bHasFog = bHasFog;
    }

    // Assigns creates tileNodes 2D array
    private TileNode[,] AssignNodes()
    {
        TileNode[,] tileNodes = new TileNode[m_MaxX, m_MaxY];

        for (int i = 0; i < m_MaxX; i++)
        {
            for (int j = 0; j < m_MaxY; j++)
            {
                // for now all tiles are walkable
                tileNodes[i, j] = new TileNode(/*bIsWalkable*/ m_TileGrid[i, j].GetBiomeType() != EBiomeType.Invalid, i, j);
            }
        }

        return tileNodes;
    }

    // Creates path from one tile to another
    private Queue<Vector3Int> CreatePath(Vector3Int from, Vector3Int to)
    {
        int xPos = from.x;
        int yPos = from.y;
        int targetXPos = to.x;
        int targetYPos = to.y;

        TileNode[,] tileNodes = AssignNodes();
        TileNode start = tileNodes[xPos, yPos];
        TileNode target = tileNodes[targetXPos, targetYPos];

        // Use the current location traversal rate for fog
        float currentTraversalRate = m_TileGrid[from.x, from.y].GetTraversalRate();

        List<TileNode> open = new List<TileNode>();
        HashSet<TileNode> closed = new HashSet<TileNode>();
        open.Add(start);

        while (open.Count > 0)
        {
            TileNode current = open[0];
            for (int i = 1; i < open.Count; i++)
            {
                if (open[i].fCost < current.fCost || open[i].fCost == current.fCost && open[i].hCost < current.hCost)
                {
                    current = open[i];
                }
            }

            open.Remove(current);
            closed.Add(current);

            if (current == target)
            {
                return RetracePath(start, target); ;
            }

            foreach (TileNode neighbor in GetNeighbors(tileNodes, current))
            {
                if (!neighbor.Walkable || closed.Contains(neighbor))
                {
                    continue;
                }

                // Only use movement mod if the tile has already been seen, if we have fog on
                STerrainTile terrainTile = m_TileGrid[neighbor.X, neighbor.Y];
                float mod = (!m_bHasFog || terrainTile.IsSeen()) ? terrainTile.GetTraversalRate() : currentTraversalRate;

                float newMovementCost = current.gCost + GetDistance(current, neighbor) * mod;
                if (newMovementCost < neighbor.gCost || !open.Contains(neighbor))
                {
                    neighbor.gCost = newMovementCost;
                    neighbor.hCost = GetDistance(neighbor, target);
                    neighbor.Parent = current;

                    if (!open.Contains(neighbor))
                        open.Add(neighbor);
                }
            }
        }

        return null;
    }

    // Retraces the path from end to start by going through the parents
    private Queue<Vector3Int> RetracePath(TileNode start, TileNode end)
    {
        List<TileNode> nodePath = new List<TileNode>();
        TileNode current = end;

        while (current != start)
        {
            nodePath.Add(current);
            current = current.Parent;
        }

        nodePath.Reverse();

        Queue<Vector3Int> path = new Queue<Vector3Int>();
        path.Enqueue(new Vector3Int(start.X, start.Y));

        foreach (TileNode node in nodePath)
        {
            path.Enqueue(new Vector3Int(node.X, node.Y));
        }

        return path;
    }

    // Calculates the "distance" between two tiles
    private int GetDistance(TileNode a, TileNode b)
    {
        int distX = Mathf.Abs(a.X - b.X);
        int distY = Mathf.Abs(a.Y - b.Y);

        if (m_Diagonals)
        {
            if (distX > distY)
            {
                return 14 * (distY) + 10 * (distX - distY);
            }

            return 14 * (distX) + 10 * (distY - distX);
        }
        else
        {
            if (distX > distY)
            {
                return 10 * (distY) + 10 * (distX);
            }

            return 10 * (distX) + 10 * (distY);
        }
    }

    // Gets neighbors of current node
    private List<TileNode> GetNeighbors(TileNode[,] nodes, TileNode node)
    {
        List<TileNode> neighbors = new List<TileNode>();
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                if (m_Diagonals)
                {
                    if (i == 0 && j == 0)
                    {
                        continue;
                    }
                }
                else
                {
                    if (i == 0 && j == 0 || Mathf.Abs(i) == 1 && Mathf.Abs(j) == 1)
                    {
                        continue;
                    }
                }

                int tempX = node.X + i;
                int tempZ = node.Y + j;
                if (tempX >= 0 && tempX < m_MaxX && tempZ >= 0 && tempZ < m_MaxY)
                {
                    neighbors.Add(nodes[tempX, tempZ]);
                }
            }
        }
        return neighbors;
    }

    // Gets the path array
    public Queue<Vector3Int> GetPath(Vector3Int from, Vector3Int to)
    {
        return CreatePath(from, to);
    }

    // Tile node for A* pathing
    private class TileNode
    {
        public bool Walkable;
        public float gCost;
        public float hCost;
        public float fCost { get { return gCost + hCost; } }
        public TileNode Parent;
        public int X;
        public int Y;

        public TileNode(bool walk, int posX, int posY)
        {
            Walkable = walk;
            X = posX;
            Y = posY;
            Parent = null;
        }
    }
}