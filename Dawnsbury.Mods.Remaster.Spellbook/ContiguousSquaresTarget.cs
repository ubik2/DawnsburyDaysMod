using Dawnsbury.Auxiliary;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Tiles;
using Dawnsbury.IO;

namespace Dawnsbury.Mods.Remaster.Spellbook
{
    /// <summary>
    /// This class acts like an AreaTarget, but allows the caster to select the area incrementally.
    /// Each subsequent target square must be adjacent to another target square.
    /// Since we act like the AreaTarget class, we also need the IncludeOnlyIf property.
    /// </summary>
    public class ContiguousSquaresTarget : GeneratorTarget
    {

        public ContiguousSquaresTarget(int range, int distance)
        {
            this.distance = distance;
            this.range = range;
        }

        public Func<ContiguousSquaresTarget, Creature, bool>? IncludeOnlyIf;
        public override bool IsAreaTarget => true;
        private int distance;
        private int range;

        private static bool IsAdjacent(Tile a, Tile b)
        {
            return ((a.X == b.X && Math.Abs(a.Y - b.Y) == 1) || (a.Y == b.Y && Math.Abs(a.X - b.X) == 1));
        }

        public override GeneratedTargetInSequence? GenerateNextTarget()
        {
            List<Tile> chosenTiles = OwnerAction.ChosenTargets.ChosenTiles;
            if (chosenTiles.Count == 0)
            {
                return new GeneratedTargetInSequence(Tile((caster, tile) =>
                {
                    if (!tile.AlwaysBlocksMovement)
                    {
                        Tile occupies = caster.Occupies;
                        if (occupies != null && occupies.DistanceTo(tile) <= range)
                        {
                            return (int)caster.Occupies.HasLineOfEffectToIgnoreLesser(tile) < 4;
                        }
                    }

                    return false;
                }, null));
            }
            if (chosenTiles.Count >= distance)
            {
                // Once we've chosen all our tiles, we will add all of the creatues in the tiles that match our IncludeOnlyIf filter.
                // This could have been handled by setting the TileTarget.WithAlsoSelectCreatures if we always wanted to include them, but this gives us a filter.
                IEnumerable<Creature> chosenTileCreatures = chosenTiles
                    .Where((Tile tile) => tile.PrimaryOccupant != null && (IncludeOnlyIf == null || IncludeOnlyIf(this, tile.PrimaryOccupant!)))
                    .Select((Tile tile) => tile.PrimaryOccupant!);
                OwnerAction.ChosenTargets.ChosenCreatures.AddRange(chosenTileCreatures);
                OwnerAction.ChosenTargets.ChosenCreatures.RemoveDuplicates();
                return null;
            }
            Tile from = chosenTiles.Last();
            return new GeneratedTargetInSequence(Tile((caster, tile) =>
            {
                // We can't pass the span through walls, and we must be adjacent (not diagonal) to an existing tile, and we can't pick the same tile again
                return !tile.AlwaysBlocksLineOfEffect && chosenTiles.Any((existingTile) => IsAdjacent(tile, existingTile) && !chosenTiles.Contains(tile));
            }, null));
        }

        public ContiguousSquaresTarget WithIncludeOnlyIf(Func<GeneratorTarget, Creature, bool>? includeOnlyIf)
        {
            this.IncludeOnlyIf = includeOnlyIf;
            return this;
        }
    }
}
