using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Tiles;

namespace Dawnsbury.Mods.Remaster.Spellbook
{
    public class PathTarget : GeneratorTarget
    {
        public PathTarget(Tile originTile, int distance)
        {
            this.distance = distance;
            this.originTile = originTile;
        }
        public override bool IsAreaTarget => true;
        private Tile originTile;
        private int distance;

        private Tuple<int, bool> PathLength(List<Tile> tiles)
        {
            if (tiles.Count == 0)
            {
                return new Tuple<int, bool>(0, false);
            }
            int length = 0;
            bool diagonal = false;
            Tile last = tiles.First();
            foreach (Tile tile in tiles)
            {
                if (Math.Abs(tile.X - last.X) == 1 && Math.Abs(tile.Y - last.Y) == 1)
                {
                    if (diagonal)
                    {
                        length += 2;
                        diagonal = false;
                    }
                    else
                    {
                        length++;
                        diagonal = true;
                    }
                }
                else if (Math.Abs(tile.X - last.X) == 1 || Math.Abs(tile.Y - last.Y) == 1)
                {
                    length++;
                }
                last = tile;
            }
            return new Tuple<int, bool>(length, diagonal);
        }

        public override GeneratedTargetInSequence? GenerateNextTarget()
        {
            List<Tile> chosenTiles = OwnerAction.ChosenTargets.ChosenTiles;
            if (chosenTiles.Count == 0)
            {
                chosenTiles.Add(originTile);
            }
            Tuple<int, bool> pathLength = PathLength(chosenTiles);
            if (pathLength.Item1 >= distance)
                return null;
            Tile from = chosenTiles.Last();
            bool canStillMoveOnDiagonal = !pathLength.Item2 || pathLength.Item1 <= distance - 2;
            return new GeneratedTargetInSequence(Tile((caster, tile) =>
            {
                if (tile.AlwaysBlocksLineOfEffect || from.DistanceTo(tile) != 1)
                {
                    return false;
                }
                else if (canStillMoveOnDiagonal || from.X == tile.X || from.Y == tile.Y)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }, null).WithAlsoSelectCreatures());
        }
    }
}
