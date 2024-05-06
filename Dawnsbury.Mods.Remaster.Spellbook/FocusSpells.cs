using Dawnsbury.Audio;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Core.Roller;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.Animations;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;

namespace Dawnsbury.Mods.Remaster.Spellbook
{
    public static class FocusSpells
    {
        public static void RegisterSpells()
        {

            RegisterClericSpells();
            RegisterDruidSpells();
        }

        static void RegisterClericSpells()
        {
            // Fire Ray leaves an effect on the ground instead of persistent damage in the remaster
            ModManager.ReplaceExistingSpell(SpellId.FireRay, 1, ((spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                const int heightenStep = 1;
                int heightenIncrements = spellLevel - 1;
                return Spells.CreateModern(IllustrationName.FireRay, "Fire Ray", new[] { Trait.Uncommon, Trait.Attack, Trait.Cleric, Trait.Concentrate, Trait.Fire, Trait.Focus, Trait.Manipulate, RemasterSpells.Trait.Remaster },
                    "A blazing band of fire arcs through the air, lighting your opponent and the ground they stand upon on fire.",
                    "Make a spell attack roll against the target's AC. The ray deals " + S.HeightenedVariable(2 + 2 * heightenIncrements, 2) + "d6 fire damage on a hit (or double damage on a critical hit).\n" +
                    "On any result other than a critical failure, the ground in the target's space catches fire, dealing " + S.HeightenedVariable(1 + heightenIncrements, 1) + "d6 fire damage to each creature that ends its turn in one of the squares." +
                    S.HeightenText(spellLevel, 1, inCombat, "{b}Heightened (+" + heightenStep + "){/b} The ray's initial damage increases by 2d6, and the fire damage dealt by the burning space increases by 1d6."),
                    Target.Ranged(12), spellLevel, null).WithSpellAttackRoll().WithSoundEffect(SfxName.FireRay)
                .WithProjectileCone(IllustrationName.FireRay, 15, ProjectileKind.Ray)
                .WithEffectOnEachTarget(async (CombatAction action, Creature caster, Creature target, CheckResult checkResult) =>
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
                            WhenExpires = (_) =>
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

            // Moonbeam does a bit more damage in the remaster
            ModManager.ReplaceExistingSpell(SpellId.Moonbeam, 1, ((spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                int heightenIncrements = spellLevel - 1;
                return Spells.CreateModern(IllustrationName.Moonbeam, "Moonbeam", new[] { Trait.Uncommon, Trait.Cleric, Trait.Concentrate, Trait.Fire, Trait.Focus, Trait.Light, Trait.Manipulate, RemasterSpells.Trait.Remaster },
                    "You shine a ray of moonlight.",
                    "Make a spell attack roll. The beam of light deals " + S.HeightenedVariable(2 + heightenIncrements, 2) + "d6 fire damage." + S.FourDegreesOfSuccess("The beam deals double damage, and the target is dazzled for 1 minute.", "The beam deals full damage, and the target is dazzled for 1 round.", null, null) +
                    S.HeightenedDamageIncrease(1, inCombat, "1d6"),
                    Target.Ranged(24), spellLevel, null).WithSpellAttackRoll().WithSoundEffect(SfxName.MagicMissile)
                .WithEffectOnEachTarget(async (CombatAction spell, Creature caster, Creature target, CheckResult result) =>
                {
                    await CommonSpellEffects.DealAttackRollDamage(spell, caster, target, result, (2 + heightenIncrements) + "d6", DamageKind.Fire);
                    if (result >= CheckResult.Success)
                    {
                        QEffect qEffect = QEffect.Dazzled();
                        if (result == CheckResult.CriticalSuccess)
                        {
                            qEffect.WithExpirationNever();
                        }
                        else
                        {
                            qEffect.WithExpirationAtStartOfSourcesTurn(caster, 1);
                        }

                        target.AddQEffect(qEffect);
                    }
                });
            }));

            // Touch of Undeath just has some wording changes to reflect the change from Positive/Negative to Vitality/Void
            ModManager.ReplaceExistingSpell(SpellId.TouchOfUndeath, 1, ((spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                return Spells.CreateModern(IllustrationName.ChillTouch, "Touch of Undeath", new[] { Trait.Uncommon, Trait.Cleric, Trait.Focus, Trait.Manipulate, RemasterSpells.Trait.Void },
                    "You attack the target's life force with undeath, dealing 1d6 void damage.",
                    "The target must attempt a Fortitude save." + S.FourDegreesOfSuccess("The target is unaffected.", "The target takes half damage.", "The target takes full damage, and vitality effects heal it only half as much as normal for 1 round.", "The target takes double damage, and vitality effects heal it only half as much as normal for 1 minute.") +
                    S.HeightenedDamageIncrease(spellLevel, inCombat, "1d6"),
                    Target.Melee().WithAdditionalConditionOnTargetCreature(new LivingCreatureTargetingRequirement()), spellLevel, SpellSavingThrow.Basic(Defense.Fortitude)).WithActionCost(1).WithSoundEffect(SfxName.ChillTouch)
                .WithEffectOnEachTarget(async (CombatAction spell, Creature caster, Creature target, CheckResult result) =>
                {
                    await CommonSpellEffects.DealBasicDamage(spell, caster, target, result, spell.SpellLevel + "d6", DamageKind.Negative);
                    if (result <= CheckResult.Failure)
                    {
                        QEffect qEffect3 = new QEffect("Touched by undeath", "Vitality effects heal you only half as much as normal", ExpirationCondition.ExpiresAtStartOfSourcesTurn, caster, IllustrationName.ChillTouch)
                        {
                            HalveHealingFromEffects = (QEffect qfSelf, CombatAction? healingEffect) => healingEffect?.HasTrait(RemasterSpells.Trait.Vitality) ?? false
                        }.WithExpirationOneRoundOrRestOfTheEncounter(caster, result == CheckResult.CriticalFailure);
                        target.AddQEffect(qEffect3);
                    }
                });
            }));
        }

        static void RegisterDruidSpells()
        {
            // Tempest Surge loses the persistent damage in the remaster
            ModManager.ReplaceExistingSpell(SpellId.TempestSurge, 1, ((spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                return Spells.CreateModern(IllustrationName.TempestSurge, "Tempest Surge", new[] { Trait.Uncommon, Trait.Air, Trait.Concentrate, Trait.Druid, Trait.Electricity, Trait.Focus, Trait.Manipulate, RemasterSpells.Trait.Remaster },
                    "You surround a foe in a swirling storm of violent winds, roiling clouds, and crackling lightning.",
                    "The storm deals " + S.HeightenedVariable(spellLevel, 1) + "d12 electricity damage to the target with a basic Reflex save. On a failure, the target is also clumsy 2 for 1 round." +
                    S.HeightenText(spellLevel, 1, inCombat, "{b}Heightened (+1){/b} The initial damage increases by 1d12."),
                    Target.Ranged(6), spellLevel, SpellSavingThrow.Basic(Defense.Reflex)).WithSoundEffect(SfxName.ElectricBlast)
                .WithEffectOnEachTarget(async (CombatAction spell, Creature caster, Creature target, CheckResult result) =>
                {
                    await CommonSpellEffects.DealBasicDamage(spell, caster, target, result, spellLevel + "d12", DamageKind.Electricity);
                    if (result <= CheckResult.Failure)
                    {
                        target.AddQEffect(QEffect.Clumsy(2).WithExpirationAtStartOfSourcesTurn(caster, 1));
                    }
                });
            }));
        }
    }
}
