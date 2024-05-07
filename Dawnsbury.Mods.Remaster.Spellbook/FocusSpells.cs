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
using Dawnsbury.Core.Possibilities;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.Remaster.Spellbook
{
    public static class FocusSpells
    {
        public static void RegisterSpells()
        {

            RegisterClericSpells();
            RegisterDruidSpells();
            RegisterWizardSpells();
        }

        static void RegisterClericSpells()
        {
            // Fire Ray leaves an effect on the ground instead of persistent damage in the remaster
            ModManager.ReplaceExistingSpell(SpellId.FireRay, 1, (spellcaster, spellLevel, inCombat, spellInformation) =>
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
            });

            // Moonbeam does a bit more damage in the remaster
            ModManager.ReplaceExistingSpell(SpellId.Moonbeam, 1, (spellcaster, spellLevel, inCombat, spellInformation) =>
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
            });

            // Touch of Undeath just has some wording changes to reflect the change from Positive/Negative to Vitality/Void
            ModManager.ReplaceExistingSpell(SpellId.TouchOfUndeath, 1, (spellcaster, spellLevel, inCombat, spellInformation) =>
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
            });
        }

        static void RegisterDruidSpells()
        {
            // Tempest Surge loses the persistent damage in the remaster
            ModManager.ReplaceExistingSpell(SpellId.TempestSurge, 1, (spellcaster, spellLevel, inCombat, spellInformation) =>
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
            });
        }

        static void RegisterWizardSpells()
        {
            // Protective Wards is essentially the same as Protective Ward
            RemasterSpells.ReplaceLegacySpell(SpellId.ProtectiveWard, "ProtectiveWards", 1, (spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                return Spells.CreateModern(IllustrationName.Abjuration, "Protective Wards", new[] { Trait.Uncommon, Trait.Aura, Trait.Focus, Trait.Manipulate, Trait.Wizard, RemasterSpells.Trait.Remaster },
                    "You expand a ring of glyphs that shields your allies.",
                    "You and any allies in the area gain a +1 status bonus to AC. Each time you Sustain the spell, the emanation's radius increases by 5 feet, to a maximum of 30 feet.",
                    Target.Self(), spellLevel, null).WithSoundEffect(SfxName.Abjuration).WithActionCost(1)
                    .WithEffectOnEachTarget(async (spell, self, target, result) =>
                    {
                        AuraAnimation auraAnimation = self.AnimationData.AddAuraAnimation(IllustrationName.BlessCircle, 1f);
                        QEffect qEffect = new QEffect("Protective Wards", "[this condition has no description]", ExpirationCondition.ExpiresAtEndOfSourcesTurn, self, IllustrationName.None)
                        {
                            CannotExpireThisTurn = true,
                            WhenExpires = (qfBless) => auraAnimation.MoveTo(0.0f),
                            Tag = 1,
                            ProvideContextualAction = (QEffect qfBless) =>
                            {
                                if (qfBless?.Tag != null)
                                {
                                    int tag = (int)qfBless.Tag;
                                    if (qfBless.CannotExpireThisTurn && tag >= 6)
                                        return null;
                                    bool canIncreaseRadius = tag < 6;
                                    return new ActionPossibility(new CombatAction(qfBless.Owner, IllustrationName.Abjuration, qfBless.CannotExpireThisTurn ? "Increase Protective Wards radius" : "Sustain" + (canIncreaseRadius ? " and increase Protective Wards radius" : ""), new[] { Trait.Concentrate },
                                        (canIncreaseRadius ? "Increase the radius of the Protective Wards emanation by 5 feet." : "") + (!qfBless.CannotExpireThisTurn ? "\n\nThis will also extend the duration of the spell until the end of your next turn." : ""), Target.Self())
                                        .WithEffectOnSelf((_) =>
                                        {
                                            qfBless.CannotExpireThisTurn = true;
                                            if (!canIncreaseRadius)
                                            {
                                                return;
                                            }
                                            int emanationRange = tag + 1;
                                            qfBless.Tag = emanationRange;
                                            auraAnimation.MoveTo(emanationRange);
                                        })).WithPossibilityGroup("Maintain an activity");
                                }
                                else
                                {
                                    return null;
                                }
                            }
                        };
                        auraAnimation.Color = Color.Green;
                        qEffect.StateCheck = (qfBless) =>
                        {
                            if (qfBless?.Tag != null)
                            {
                                int emanationSize = (int)qfBless.Tag;
                                foreach (Creature creature in qfBless.Owner.Battle.AllCreatures.Where((Creature cr) => cr.DistanceTo(qfBless.Owner) <= emanationSize && cr.FriendOf(qfBless.Owner)))
                                {
                                    creature.AddQEffect(new QEffect("Protective Wards", "You gain a +1 status bonus to AC.", ExpirationCondition.Ephemeral, qfBless.Owner, IllustrationName.Bless)
                                    {
                                        BonusToDefenses = (_3, _4, defense) => defense != Defense.AC ? null : new Bonus(1, BonusType.Status, "Protective Wards")
                                    });
                                }
                            }
                        };
                        self.AddQEffect(qEffect);
                    });
            });

            // Force Bolt is essentially the same

            // Earthworks
            RemasterSpells.RegisterNewSpell("Earthworks", 1, (spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                return Spells.CreateModern(IllustrationName.PummelingRubble, "Earth Works", new[] { Trait.Uncommon, Trait.Concentrate, Trait.Earth, Trait.Focus, Trait.Manipulate, Trait.Wizard, RemasterSpells.Trait.Remaster },
                    "With a ripple of earth, you raise small barriers from the ground.",
                    "The ground in the area becomes difficult terrain. The spell's area is a 5-foot burst if you spent 1 action to cast it, a 10-foot burst if you spent 2 actions, or a 15-foot burst if you spent 3 actions. A creature can Interact to clear the barriers from one 5-foot square adjacent to it.",
                    Target.DependsOnActionsSpent(Target.Burst(12, 1), Target.Burst(12, 2), Target.Burst(12, 3)), 1, null)
                .WithActionCost(-1).WithSoundEffect(SfxName.ElementalBlastEarth)
                .WithEffectOnChosenTargets(async (CombatAction spell, Creature caster, ChosenTargets targets) =>
                {
                    List<TileQEffect> effects = new List<TileQEffect>();
                    if (spell.SpellcastingSource == null)
                    {
                        throw new Exception("SpellcastingSource should not be null");
                    }
                    int spellDC = spell.SpellcastingSource.GetSpellSaveDC();
                    foreach (Tile tile in targets.ChosenTiles)
                    {
                        TileQEffect item = new TileQEffect(tile)
                        {
                            StateCheck = (_) => { tile.DifficultTerrain = true; },
                            Illustration = new[] { IllustrationName.Rubble, IllustrationName.Rubble2 }.GetRandom(),
                            ExpiresAt = ExpirationCondition.Never
                        };
                        effects.Add(item);
                        tile.QEffects.Add(item);
                    }
                });
            });

            // Charming Push is mostly just a rename of Charming Words
            RemasterSpells.ReplaceLegacySpell(SpellId.CharmingWords, "CharmingPush", 1, (spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                return Spells.CreateModern(IllustrationName.Enchantment, "Charming Push", new[] { Trait.Uncommon, Trait.Concentrate, Trait.Focus, Trait.Incapacitation, Trait.Mental, Trait.Wizard, RemasterSpells.Trait.Remaster },
                    "You push at the target's mind to deflect their ire. The target must attempt a Will save.",
                    RemasterSpells.StripInitialWhitespace(S.FourDegreesOfSuccess("The target is unaffected.", "The target takes a –1 circumstance penalty to attack rolls and damage rolls against you.", "The target can't use hostile actions against you.", "The target is stunned 1 and can't use hostile actions against you.")),
                    Target.Ranged(6), spellLevel, SpellSavingThrow.Standard(Defense.Will)).WithSoundEffect(SfxName.Bless).WithActionCost(1)
                    .WithEffectOnEachTarget(async (spell, caster, target, result) =>
                    {
                        switch (result)
                        {
                            case CheckResult.CriticalFailure:
                            case CheckResult.Failure:
                                if (result == CheckResult.CriticalFailure)
                                    target.AddQEffect(QEffect.Stunned(1));
                                target.AddQEffect(new QEffect("Charming Push", "You can't target " + caster?.ToString() + ".", ExpirationCondition.ExpiresAtStartOfSourcesTurn, caster, IllustrationName.Enchantment).AddGrantingOfTechnical((Creature c) => c == caster, (QEffect qfTechnical) => qfTechnical.PreventTargetingBy = ((CombatAction ca) => ca.Owner != target ? null : "charming push")));
                                break;
                            case CheckResult.Success:
                                target.AddQEffect(new QEffect("Charming Push", "You have -1 on attack and damage against " + caster?.ToString() + ".", ExpirationCondition.ExpiresAtStartOfSourcesTurn, caster, IllustrationName.Enchantment)
                                {
                                    BonusToAttackRolls = ((_, power, newTarget) => !power.HasTrait(Trait.Attack) || newTarget != caster ? null : new Bonus(-1, BonusType.Circumstance, "Charming Words"))
                                });
                                break;
                        }
                    });
            });

            RemasterSpells.RegisterNewSpell("ScrambleBody", 1, (spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                return Spells.CreateModern(IllustrationName.Transmutation, "Scramble Body", new[] { Trait.Uncommon, Trait.Concentrate, Trait.Focus, Trait.Incapacitation, Trait.Mental, Trait.Wizard, RemasterSpells.Trait.Remaster },
                    "Your magic throws the creature's biology into disarray, inducing nausea, fever, and other unpleasant conditions.",
                    RemasterSpells.StripInitialWhitespace(S.FourDegreesOfSuccess(null, "The target is unaffected.", "The target becomes sickened 1.", "The target becomes sickened 2 and slowed 1 as long as it's sickened.")),
                    Target.Ranged(6).WithAdditionalConditionOnTargetCreature(new LivingCreatureTargetingRequirement()), spellLevel, SpellSavingThrow.Standard(Defense.Fortitude)).WithSoundEffect(SfxName.Boneshaker).WithActionCost(2)
                    .WithEffectOnEachTarget(async (spell, caster, target, result) =>
                    {
                        switch (result)
                        {
                            case CheckResult.CriticalFailure:
                                QEffect slowed = QEffect.Slowed(1).WithExpirationNever();
                                QEffect sickened = QEffect.Sickened(2, spell.SpellcastingSource!.GetSpellSaveDC());
                                sickened.WhenExpires = (_) => slowed.ExpiresAt = ExpirationCondition.Immediately;
                                target.AddQEffect(sickened);
                                target.AddQEffect(slowed);
                                break;
                            case CheckResult.Failure:
                                target.AddQEffect(QEffect.Sickened(1, spell.SpellcastingSource!.GetSpellSaveDC()));
                                break;
                        }
                    });
            });

            // Fortify Summoning is mostly just a rename of Augment Summoning
            RemasterSpells.ReplaceLegacySpell(SpellId.AugmentSummoning, "FortifySummoning", 1, (spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                return Spells.CreateModern(IllustrationName.Conjuration, "Fortify Summoning", new[] { Trait.Uncommon, Trait.Concentrate, Trait.Focus, Trait.Wizard, RemasterSpells.Trait.Remaster },
                    "As you call a creature to your side, your magic transforms its body, heightening its ferocity and fortifying its resilience.",
                    "The target gains a +1 status bonus to all checks and DCs (including its AC) for the duration of its summoning, up to 1 minute.",
                    Target.RangedFriend(6).WithAdditionalConditionOnTargetCreature((a, d) => !d.QEffects.Any((qf) => (qf.Id == QEffectId.SummonedBy && qf.Source == a)) ? Usability.NotUsableOnThisCreature("not your summon") : Usability.Usable), spellLevel, null).WithSoundEffect(SfxName.MinorAbjuration).WithActionCost(1)
                    .WithEffectOnEachTarget(async (spell, caster, target, result) =>
                    {
                        target.AddQEffect(new QEffect("Fortify Summoning", "You have +1 to all checks.", ExpirationCondition.Never, caster, IllustrationName.Conjuration)
                        {
                            BonusToAllChecksAndDCs = (Func<QEffect, Bonus>)(_ => new Bonus(1, BonusType.Status, "Augment Summoning"))
                        });
                    });
            });

            // Hand of the Apprentice is essentially the same
        }
    }
}
