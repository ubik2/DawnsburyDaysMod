using Dawnsbury.Audio;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core;
using System.Collections.Generic;
using System.Linq;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Core.Roller;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.Animations;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;

namespace Dawnsbury.Mods.Remaster.Spellbook
{
    internal class FocusSpells
    {
        public static void RegisterSpells()
        {
            ModManager.ReplaceExistingSpell(SpellId.FireRay, 1, ((spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                const int heightenStep = 1;
                int heightenIncrements = spellLevel - 1;
                return Spells.CreateModern(IllustrationName.FireRay, "Fire Ray", new[] { Trait.Uncommon, Trait.Attack, Trait.Cleric, Trait.Concentrate, Trait.Fire, Trait.Focus, Trait.Manipulate, RemasterSpells.RemasterTrait },
                    "A blazing band of fire arcs through the air, lighting your opponent and the ground they stand upon on fire.",
                    "Make a spell attack roll against the target's AC. The ray deals " + S.HeightenedVariable(2 + 2 * heightenIncrements, 2) + "d6 fire damage on a hit (or double damage on a critical hit).\n" +
                    "On any result other than a critical failure, the ground in the target's space catches fire, dealing " + S.HeightenedVariable(1 + heightenIncrements, 1) + "d6 fire damage to each creature that ends its turn in one of the squares." +
                    S.HeightenText(spellLevel, 1, inCombat, "{b}Heightened (+" + heightenStep + "){/b} The ray's initial damage increases by 2d6, and the fire damage dealt by the burning space increases by 1d6."),
                    Target.Ranged(12), spellLevel, null).WithSpellAttackRoll().WithSoundEffect(SfxName.FireRay)
                .WithProjectileCone(IllustrationName.FireRay, 15, ProjectileKind.Ray)
                .WithEffectOnEachTarget(async delegate (CombatAction action, Creature caster, Creature target, CheckResult checkResult)
                {
                    await CommonSpellEffects.DealAttackRollDamage(action, caster, target, checkResult, (2 + 2 * heightenIncrements) + "d6", DamageKind.Fire);
                    if (checkResult != CheckResult.CriticalFailure)
                    {
                        Creature source = caster;
                        List<TileQEffect> listOfDependentEffects = new List<TileQEffect>();
                        // I treat this as a list, since a large target will occupy more than one tile
                        // Dawnsbury doesn't currently do this, though
                        foreach (Tile tile in new[] { target.Occupies })
                        {
                            TileQEffect tileEffect = new TileQEffect(tile)
                            {
                                Illustration = new[] { IllustrationName.FireTile1, IllustrationName.FireTile2, IllustrationName.FireTile3, IllustrationName.FireTile4 }.GetRandom(),
                                StateCheck = (self) =>
                                {
                                    self.Owner.PrimaryOccupant?.AddQEffect(new QEffect(ExpirationCondition.Ephemeral)
                                    {
                                        EndOfYourTurn = async (_, creature) =>
                                        {
                                            await creature.DealDirectDamage(null, DiceFormula.FromText((1 + heightenIncrements) + "d6", "Fire Ray fire tile"), creature, CheckResult.Failure, DamageKind.Fire);
                                        }
                                    });
                                }
                            };

                            listOfDependentEffects.Add(tileEffect);
                            tile.QEffects.Add(tileEffect);
                        }
                        QEffect cleanupEffect = new QEffect(ExpirationCondition.ExpiresAtEndOfYourTurn)
                        {
                            Source = caster,
                            WhenExpires = delegate
                            {
                                foreach (TileQEffect tileEffect in listOfDependentEffects.ToList())
                                {
                                    tileEffect.Owner.QEffects.Remove(tileEffect);
                                }
                            }
                        };
                        target.AddQEffect(cleanupEffect);
                    }
                });
            }));
        }
    }
}
