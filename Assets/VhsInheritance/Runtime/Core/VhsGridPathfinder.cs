using System.Collections.Generic;
using UnityEngine;

namespace VhsInheritance
{
    public static class VhsGridPathfinder
    {
        private sealed class Node
        {
            public Vector2Int cell;
            public float g;
            public float h;
            public Node parent;
            public float F => g + h;
        }

        private static readonly Vector2Int[] Directions =
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0),
            new Vector2Int(0, 1), new Vector2Int(0, -1),
            new Vector2Int(1, 1), new Vector2Int(1, -1),
            new Vector2Int(-1, 1), new Vector2Int(-1, -1)
        };

        public static List<Vector2> FindPath(Vector2 start, Vector2 goal, Rect bounds, float cellSize, IReadOnlyList<Rect> obstacles)
        {
            cellSize = Mathf.Max(0.2f, cellSize);
            if (!bounds.Contains(start) || !bounds.Contains(goal) || IsBlocked(goal, obstacles))
                return new List<Vector2>();
            Vector2Int gridSize = new Vector2Int(
                Mathf.Max(1, Mathf.CeilToInt(bounds.width / cellSize)),
                Mathf.Max(1, Mathf.CeilToInt(bounds.height / cellSize)));

            Vector2Int startCell = WorldToCell(start, bounds, cellSize, gridSize);
            Vector2Int goalCell = WorldToCell(goal, bounds, cellSize, gridSize);
            List<Node> open = new List<Node>();
            Dictionary<Vector2Int, Node> all = new Dictionary<Vector2Int, Node>();
            HashSet<Vector2Int> closed = new HashSet<Vector2Int>();
            Node startNode = new Node { cell = startCell, g = 0f, h = Heuristic(startCell, goalCell) };
            open.Add(startNode);
            all[startCell] = startNode;

            int guard = gridSize.x * gridSize.y * 4;
            while (open.Count > 0 && guard-- > 0)
            {
                int bestIndex = 0;
                for (int i = 1; i < open.Count; i++)
                {
                    if (open[i].F < open[bestIndex].F ||
                        Mathf.Approximately(open[i].F, open[bestIndex].F) && open[i].h < open[bestIndex].h)
                    {
                        bestIndex = i;
                    }
                }

                Node current = open[bestIndex];
                open.RemoveAt(bestIndex);
                if (current.cell == goalCell)
                {
                    return Reconstruct(current, bounds, cellSize, goal);
                }

                closed.Add(current.cell);
                for (int i = 0; i < Directions.Length; i++)
                {
                    Vector2Int nextCell = current.cell + Directions[i];
                    if (nextCell.x < 0 || nextCell.y < 0 || nextCell.x >= gridSize.x || nextCell.y >= gridSize.y || closed.Contains(nextCell))
                    {
                        continue;
                    }

                    Vector2 nextWorld = CellToWorld(nextCell, bounds, cellSize);
                    if (nextCell != goalCell && IsBlocked(nextWorld, obstacles))
                    {
                        continue;
                    }

                    bool diagonal = Directions[i].x != 0 && Directions[i].y != 0;
                    if (diagonal && (IsBlocked(CellToWorld(current.cell + new Vector2Int(Directions[i].x, 0), bounds, cellSize), obstacles) ||
                        IsBlocked(CellToWorld(current.cell + new Vector2Int(0, Directions[i].y), bounds, cellSize), obstacles)))
                        continue;
                    float tentativeG = current.g + (diagonal ? 1.4142f : 1f);
                    if (!all.TryGetValue(nextCell, out Node nextNode))
                    {
                        nextNode = new Node
                        {
                            cell = nextCell,
                            g = tentativeG,
                            h = Heuristic(nextCell, goalCell),
                            parent = current
                        };
                        all[nextCell] = nextNode;
                        open.Add(nextNode);
                    }
                    else if (tentativeG < nextNode.g)
                    {
                        nextNode.g = tentativeG;
                        nextNode.parent = current;
                        if (!open.Contains(nextNode))
                        {
                            open.Add(nextNode);
                        }
                    }
                }
            }

            return new List<Vector2>();
        }

        private static bool IsBlocked(Vector2 point, IReadOnlyList<Rect> obstacles)
        {
            if (obstacles == null)
            {
                return false;
            }

            for (int i = 0; i < obstacles.Count; i++)
            {
                if (obstacles[i].Contains(point))
                {
                    return true;
                }
            }

            return false;
        }

        private static float Heuristic(Vector2Int a, Vector2Int b)
        {
            return Vector2Int.Distance(a, b);
        }

        private static Vector2Int WorldToCell(Vector2 world, Rect bounds, float cellSize, Vector2Int gridSize)
        {
            int x = Mathf.Clamp(Mathf.FloorToInt((world.x - bounds.xMin) / cellSize), 0, gridSize.x - 1);
            int y = Mathf.Clamp(Mathf.FloorToInt((world.y - bounds.yMin) / cellSize), 0, gridSize.y - 1);
            return new Vector2Int(x, y);
        }

        private static Vector2 CellToWorld(Vector2Int cell, Rect bounds, float cellSize)
        {
            return new Vector2(bounds.xMin + (cell.x + 0.5f) * cellSize, bounds.yMin + (cell.y + 0.5f) * cellSize);
        }

        private static List<Vector2> Reconstruct(Node node, Rect bounds, float cellSize, Vector2 exactGoal)
        {
            List<Vector2> result = new List<Vector2>();
            Node current = node;
            while (current != null)
            {
                result.Add(CellToWorld(current.cell, bounds, cellSize));
                current = current.parent;
            }

            result.Reverse();
            if (result.Count > 0)
            {
                result[result.Count - 1] = exactGoal;
            }

            return result;
        }
    }
}
