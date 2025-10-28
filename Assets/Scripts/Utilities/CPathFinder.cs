using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CPathFinder
{
    private STerrainTile[,] m_TileGrid;
    private EBiomeType tiletype;
    bool m_Diagonals;
    int maxX, maxY;

    // Constructor takes in tilegrid and diagonal
    public CPathFinder(bool diag, STerrainTile[,] tileGrid)
    {
        m_TileGrid = tileGrid;
        m_Diagonals = diag;

        maxX = m_TileGrid.GetLength(0);
        maxY = m_TileGrid.GetLength(1);
    }

    // Assigns creates tileNodes 2D array
    private TileNode[,] AssignNodes()
    {
        TileNode[,] tileNodes = new TileNode[maxX, maxY];

        for (int i = 0; i < maxX; i++)
        {
            for (int j = 0; j < maxY; j++)
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
                if (!neighbor.walkable || closed.Contains(neighbor))
                {
                    continue;
                }

                float mod = m_TileGrid[neighbor.x, neighbor.y].GetTraversalRate();

                float newMovementCost = current.gCost + GetDistance(current, neighbor) * mod;
                if (newMovementCost < neighbor.gCost || !open.Contains(neighbor))
                {
                    neighbor.gCost = newMovementCost;
                    neighbor.hCost = GetDistance(neighbor, target);
                    neighbor.parent = current;

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
            current = current.parent;
        }

        nodePath.Reverse();

        Queue<Vector3Int> path = new Queue<Vector3Int>();
        path.Enqueue(new Vector3Int(start.x, start.y));

        foreach (TileNode node in nodePath)
        {
            path.Enqueue(new Vector3Int(node.x, node.y));
        }

        return path;
    }

    // Calculates the "distance" between two tiles
    private int GetDistance(TileNode a, TileNode b)
    {
        int distX = Mathf.Abs(a.x - b.x);
        int distY = Mathf.Abs(a.y - b.y);

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

                int tempX = node.x + i;
                int tempZ = node.y + j;
                if (tempX >= 0 && tempX < maxX && tempZ >= 0 && tempZ < maxY)
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
        public bool walkable;
        public float gCost;
        public float hCost;
        public float fCost { get { return gCost + hCost; } }
        public TileNode parent;
        public int x;
        public int y;

        public TileNode(bool walk, int posX, int posY)
        {
            walkable = walk;
            x = posX;
            y = posY;
            parent = null;
        }
    }
}