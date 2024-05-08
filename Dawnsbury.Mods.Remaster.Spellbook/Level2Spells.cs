using Microsoft.Xna.Framework;
using Dawnsbury.Audio;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.Animations;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Display.Text;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Roller;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Core.Animations.Movement;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.Creatures.Parts;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core.StatBlocks;
using Dawnsbury.Display;

namespace Dawnsbury.Mods.Remaster.Spellbook
{
    public static class Level2Spells
    {
        // The following spells are excluded because they aren't useful enough in gameplay
        // * Animal Messenger
        // * Augury
        // * Cleanse Affliction
        // * Create Food
        // * Darkvision
        // * Deafness
        // * Dispel Magic
        // * Embed Message
        // * Environmental Endurance (formerly Endure Elements)
        // * Everlight
        // * Gecko Grip
        // * Humanoid Form
        // * Knock
        // * Marvelous Mount
        // * One with Plants
        // * Peaceful Rest
        // * Shape Wood
        // * Shatter
        // * Speak with Animals
        // * Status
        // * Sure Footing
        // * Translate
        // * Water Breathing
        // * Water Walk
        // The following spells are excluded because of their difficulty
        // * Animal Form
        // * Darkness (no light model)
        // * Enlarge (creature sizes not supported)
        // * Ghostly Carrier (creature management)
        // * Paranoia
        // * Shrink (creature sizes not supported)
        // * Silence
        // The following are in limbo
        // * Clear Mind
        // * Illusory Creature
        // * Share Life
        // * Sound Body
        public static void RegisterSpells()
        {
            // Renamed from Acid Arrow. Updated traits, description, and functionality
            RemasterSpells.ReplaceLegacySpell(SpellId.AcidArrow, "AcidGrip", 2, (spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                int heightenIncrements = (spellLevel - 2) / 2;
                return Spells.CreateModern(IllustrationName.AcidArrow, "Acid Grip", new[] { Trait.Acid, Trait.Concentrate, Trait.Manipulate, Trait.Arcane, Trait.Primal, RemasterSpells.Trait.Remaster },
                    "An ephemeral, taloned hand grips the target, burning it with magical acid.",
                    "The target takes " + S.HeightenedVariable(2 + 2 * heightenIncrements, 2) + "d8 acid damage plus " + S.HeightenedVariable(1 + heightenIncrements, 1) + "d6 persistent acid damage depending on its Reflex save. A creature taking persistent damage from this spell takes a –10-foot status bonus to its Speeds." +
                    S.FourDegreesOfSuccess("The creature is unaffected.",
                        "The creature takes half damage and no persistent damage, and the claw moves it up to 5 feet in a direction of your choice.",
                        "The creature takes full damage and persistent damage, and the claw moves it up to 10 feet in a direction of your choice.",
                        "The creature takes double damage and full persistent damage, and the claw moves it up to 20 feet in a direction of your choice."),
                    // S.HeightenText(spellLevel, 2, inCombat, "{b}Heightened (+2){/b} The initial damage increases by 2d8, and the persistent acid damage increases by 1d6."),
                    Target.Ranged(24), spellLevel, SpellSavingThrow.Basic(Defense.Reflex))
                .WithSoundEffect(SfxName.AcidSplash)
                .WithGoodnessAgainstEnemy((Target t, Creature a, Creature d) => (float)((t.OwnerAction.SpellLevel - 1) * 2) * 4.5f)
                .WithEffectOnEachTarget(async (CombatAction spell, Creature caster, Creature target, CheckResult checkResult) =>
                {
                    await CommonSpellEffects.DealBasicDamage(spell, caster, target, checkResult, (2 + 2 * heightenIncrements) + "d8", DamageKind.Acid);
                    if (checkResult <= CheckResult.Failure)
                    {
                        DiceFormula diceFormula = DiceFormula.FromText((1 + heightenIncrements) + "d6", "Persistent damage");
                        target.AddQEffect(QEffect.PersistentDamage(diceFormula, DamageKind.Acid));
                    }
                    int moveDistance = checkResult switch { CheckResult.Success => 1, CheckResult.Failure => 2, CheckResult.CriticalFailure => 4, _ => 0 };
                    if (moveDistance > 0)
                    {
                        await CommonSpellEffects.Slide(caster, target, moveDistance);
                    }
                });
            });

            // Blazing Bolts (formerly Scorching Ray)
            RemasterSpells.RegisterNewSpell("BlazingBolt", 2, (spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                Func<CreatureTarget> func = () => Target.Ranged(12, (Target tg, Creature attacker, Creature defender) => attacker.AI.DealDamage(defender, 14f, tg.OwnerAction));
                return Spells.CreateModern(IllustrationName.BurningHands, "Blazing Bolt", new[] { Trait.Attack, Trait.Concentrate, Trait.Fire, Trait.Manipulate, Trait.Arcane, Trait.Primal, RemasterSpells.Trait.Remaster },
                    "You fire a ray of heat and flame.",
                    "{b}Range{/b} 60 feet\n{b}Targets{/b} 1 or more creatures\n\n" +
                    "Make a spell attack roll against a single creature. On a hit, the target takes " + S.HeightenedVariable(spellLevel, 2) + "d6 fire damage, and on a critical hit, the target takes double damage.\n\n" +
                    "For each additional action you use when Casting the Spell, you can fire an additional ray at a different target, to a maximum of three rays targeting three different targets for 3 actions.\n\n" +
                    "These attacks each increase your multiple attack penalty, but you don't increase your multiple attack penalty until after you make all the spell attack rolls for blazing bolt. If you spend 2 or more actions Casting the Spell, the damage increases to " + S.HeightenedVariable(2 * spellLevel, 4) + "d6 fire damage on a hit, and it still deals double damage on a critical hit.",
                    //S.HeightenText(spellLevel, 2, inCombat, "{b}Heightened (+1){/b} The damage to each target increases by 1d6 for the 1-action version, or by 2d6 for the 2- and 3-action versions."),
                    Target.DependsOnActionsSpent(
                        Target.MultipleCreatureTargets(func()).WithMustBeDistinct().WithOverriddenTargetLine("1 creature", plural: false),
                        Target.MultipleCreatureTargets(func(), func()).WithMustBeDistinct().WithOverriddenTargetLine("1 or 2 creatures", plural: true),
                        Target.MultipleCreatureTargets(func(), func(), func()).WithMustBeDistinct().WithOverriddenTargetLine("1, 2 or 3 creatures", plural: true)), spellLevel, null)
                .WithActionCost(-1).WithSoundEffect(SfxName.MagicMissile)
                .WithProjectileCone(IllustrationName.MagicMissile, 15, ProjectileKind.Ray)
                .WithCreateVariantDescription((int actionCost, SpellVariant? variant) => (actionCost != 1)
                    ? ("You fire " + actionCost + " rays of heat and flame. Make a spell attack roll against a single creature. On a hit, the target takes " + S.HeightenedVariable(2 * spellLevel, 4) + "d6 fire damage, and on a critical hit, the target takes double damage.")
                    : ("You fire a ray of heat and flame. Make a spell attack roll against a single creature. On a hit, the target takes " + S.HeightenedVariable(spellLevel, 2) + "d6 fire damage, and on a critical hit, the target takes double damage."))
                .WithSpellAttackRoll()
                .WithEffectOnChosenTargets(async (CombatAction spell, Creature caster, ChosenTargets targets) =>
                {
                    string damageDice = (spell.SpentActions > 1) ? (2 * spellLevel + "d6") : (spellLevel + "d6");
                    foreach (Creature target in targets.GetTargetCreatures())
                    {
                        await CommonSpellEffects.DealAttackRollDamage(spell, caster, target, targets.CheckResults[target], damageDice, DamageKind.Fire);
                    }
                    // Each extra ray increases our attack penalty (after all rolls)
                    caster.Actions.AttackedThisManyTimesThisTurn += spell.SpentActions - 1;
                })
                .WithTargetingTooltip((CombatAction power, Creature creature, int index) =>
                {
                    string ordinal = index switch
                    {
                        0 => "first",
                        1 => "second",
                        2 => "third",
                        _ => index + "th",
                    };
                    return "Send the " + ordinal + " ray at " + creature?.ToString() + ". (" + (index + 1) + "/" + power.SpentActions + ")";
                });
            });

            // Calm (formerly Calm Emotions)
            RemasterSpells.ReplaceLegacySpell(SpellId.CalmEmotions, "Calm", 2, (spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                return Spells.CreateModern(IllustrationName.CalmEmotions, "Calm", new[] { Trait.Concentrate, Trait.Emotion, Trait.Incapacitation, Trait.Manipulate, Trait.Mental, Trait.Divine, Trait.Occult, RemasterSpells.Trait.Remaster },
                    "You forcibly calm creatures in the area, soothing them into a nonviolent state; each creature must attempt a Will save.",
                    RemasterSpells.StripInitialWhitespace(S.FourDegreesOfSuccess("The creature is unaffected.", "Calming urges impose a –1 status penalty to the creature's attack rolls.",
                    "Any emotion effects that would affect the creature are suppressed and the creature can't use hostile actions. If the target is subject to hostility from any other creature, it ceases to be affected by {i}calm{/i}.", "As failure, but hostility doesn't end the effect.")),
                    Target.Burst(24, 2), spellLevel, SpellSavingThrow.Standard(Defense.Will)).WithSoundEffect(SfxName.PureEnergyRelease)
                .WithEffectOnEachTarget(async (CombatAction spell, Creature caster, Creature target, CheckResult result) =>
                {
                    QEffect qEffect;
                    switch (result)
                    {
                        default:
                            return;
                        case CheckResult.Success:
                            qEffect = new QEffect("Calm", "You get a -1 status penalty to attack rolls.", ExpirationCondition.ExpiresAtEndOfSourcesTurn, caster, IllustrationName.CalmEmotions)
                            {
                                BonusToAttackRolls = (QEffect effect, CombatAction action, Creature? arg3) => new Bonus(-1, BonusType.Status, "Calm")
                            };
                            break;
                        case CheckResult.Failure:
                            qEffect = new QEffect("Calm", "You can't use hostile actions. If you're attacked by any other creature, the spell ends on you.", ExpirationCondition.ExpiresAtEndOfSourcesTurn, caster, IllustrationName.CalmEmotions)
                            {
                                Id = QEffectId.CalmEmotions,
                                PreventTakingAction = (CombatAction action) => (!action.WillBecomeHostileAction) ? null : "You can't use hostile actions."
                            };
                            qEffect.AddGrantingOfTechnical((Creature cr) => cr.EnemyOf(target), (QEffect qfTechnical) =>
                            {
                                qfTechnical.AfterYouTakeHostileAction = (QEffect qfTechnical2, CombatAction hostileAction) =>
                                {
                                    if (hostileAction.ChosenTargets.Targets(target))
                                    {
                                        qEffect.ExpiresAt = ExpirationCondition.Immediately;
                                    }
                                };
                            });
                            break;
                        case CheckResult.CriticalFailure:
                            qEffect = new QEffect("Calm", "You can't use hostile actions.", ExpirationCondition.ExpiresAtEndOfSourcesTurn, caster, IllustrationName.CalmEmotions)
                            {
                                Id = QEffectId.CalmEmotions,
                                PreventTakingAction = (CombatAction action) => (!action.WillBecomeHostileAction) ? null : "You can't use hostile actions."
                            };
                            break;
                    }

                    qEffect.CannotExpireThisTurn = true;
                    target.AddQEffect(qEffect);
                    QEffect? qSustainMultipleEffect = caster.QEffects.FirstOrDefault((QEffect qfc) => qfc.Id == QEffectId.SustainingMultiple && qfc.SourceAction == spell);
                    if (qSustainMultipleEffect == null)
                    {
                        qSustainMultipleEffect = QEffect.SustainingMultiple(spell);
                        caster.AddQEffect(qSustainMultipleEffect);
                    }

                    List<QEffect> list = (List<QEffect>)qSustainMultipleEffect.Tag!;
                    list.Add(qEffect);
                });
            });

            // Entangling Flora (formerly Entangle)
            RemasterSpells.RegisterNewSpell("EntanglingFlora", 2, (spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                return Spells.CreateModern(IllustrationName.FlourishingFlora, "Entangling Flora", new[] { Trait.Concentrate, Trait.Manipulate, Trait.Plant, Trait.Wood, Trait.Arcane, Trait.Primal, RemasterSpells.Trait.Remaster },
                    "Plants and fungi burst out or quickly grow, entangling creatures.",
                    "All surfaces in the area are difficult terrain. Each round that a creature starts its turn in the area, it must attempt a Reflex save. On a failure, it takes a –10-foot circumstance penalty to its Speeds until it leaves the area, and on a critical failure, it’s also immobilized for 1 round. Creatures can attempt to Escape to remove these effects." + "\n\n{i}Dismiss Entangling Flora is in Other maneuvers menu.{/i}",
                    Target.Burst(24, 4), spellLevel, null).WithSoundEffect(SfxName.Boneshaker)
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
                            AfterCreatureBeginsItsTurnHere = async (Creature creature) =>
                            {
                                CheckResult checkResult = CommonSpellEffects.RollSavingThrow(creature, CombatAction.CreateSimple(creature, "Push Through Entangling Flora"), Defense.Reflex, (_) => spellDC);
                                QEffect? entangledEffect = null;
                                if (checkResult == CheckResult.Failure)
                                {
                                    if (!creature.QEffects.Any((qEffect) => qEffect.Name == "slowed by Entangling Flora" || (qEffect.Id == QEffectId.Immobilized && qEffect.Source == caster)))
                                    {
                                        entangledEffect = new QEffect("slowed by Entangling Flora", "-10-foot circumstance penalty to Speeds", ExpirationCondition.Never, caster, IllustrationName.FlourishingFlora)
                                        {
                                            BonusToAllSpeeds = (_) => new Bonus(-2, BonusType.Circumstance, spell.Name, false),
                                            CountsAsADebuff = true
                                        };
                                    }
                                }
                                else if (checkResult == CheckResult.CriticalFailure)
                                {
                                    // If we are already slowed, remove that effect
                                    creature.RemoveAllQEffects((qEffect) => qEffect.Name == "slowed by Entangling Flora" || (qEffect.Id == QEffectId.Immobilized && qEffect.Source == caster));
                                    entangledEffect = QEffect.Immobilized().WithExpirationNever();
                                    entangledEffect.Source = caster;
                                }
                                if (entangledEffect != null)
                                {
                                    entangledEffect.ProvideContextualAction = (qEffect) => EscapeAction(creature, qEffect, spell.SpellcastingSource);
                                    entangledEffect.StateCheck = (qEffect) =>
                                    {
                                        // Remove the effect when the creature leaves the area
                                        if (!targets.ChosenTiles.Contains(creature.Occupies))
                                        {
                                            qEffect.ExpiresAt = ExpirationCondition.Immediately;
                                        }
                                    };
                                    creature.AddQEffect(entangledEffect);
                                }
                            },
                            Illustration = (new[] { IllustrationName.Spiderweb1, IllustrationName.Spiderweb2, IllustrationName.Spiderweb3, IllustrationName.Spiderweb4 }).GetRandom(),
                            ExpiresAt = ExpirationCondition.Never
                        };
                        effects.Add(item);
                        tile.QEffects.Add(item);
                    }

                    caster.AddQEffect(new QEffect
                    {
                        ProvideActionIntoPossibilitySection = (QEffect effect, PossibilitySection section) => (section.PossibilitySectionId != PossibilitySectionId.OtherManeuvers) ? null : new ActionPossibility(new CombatAction(caster, IllustrationName.FlourishingFlora, "Dismiss Entangling Flora", new[] { Trait.Concentrate }, "Dismiss this effect.", Target.Self())
                        .WithEffectOnSelf((_) =>
                        {
                            foreach (TileQEffect tileEffect in effects)
                            {
                                tileEffect.ExpiresAt = ExpirationCondition.Immediately;
                            }
                        }))
                    });
                });
            });

            // False Vitality (handle like Mystic Armor)
            RemasterSpells.RegisterNewSpell("FalseVitality", 2, (spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                CombatAction falseVitality = Spells.CreateModern(IllustrationName.Soothe, "False Vitality", new[] { Trait.Concentrate, Trait.Manipulate, Trait.Arcane, Trait.Occult, RemasterSpells.Trait.Remaster },
                    "You augment your flesh with the energies typically used to manipulate the undead.",
                    "You gain 10 temporary Hit Points." +
                    "\n\n{b}Special{/b} You can cast this spell as a free action at the beginning of the encounter.", Target.Self(), spellLevel, null).WithSoundEffect(SfxName.Abjuration).WithActionCost(2)
                .WithEffectOnEachTarget(async (CombatAction spell, Creature caster, Creature target, CheckResult checkResult) =>
                {
                    caster.GainTemporaryHP(10);
                    // Add a marker so we don't prompt them to cast it more than once
                    caster.AddQEffect(new QEffect("False Vitality", "").WithExpirationAtStartOfOwnerTurn());
                });
                falseVitality.WhenCombatBegins = (Creature caster) =>
                {
                    caster.AddQEffect(new QEffect
                    {
                        StartOfCombat = async (_) =>
                        {
                            if (!caster.QEffects.Any((qEffect) => qEffect.Name == "False Vitality") &&
                                await caster.Battle.AskForConfirmation(caster, IllustrationName.Soothe, "Do you want to cast {i}false vitality{/i} as a free action?", "Cast {i}false vitality{/i}"))
                            {
                                await caster.Battle.GameLoop.FullCast(falseVitality);
                            }
                        }
                    });
                };
                return falseVitality;
            });

            // Floating Flame (formerly Flaming Sphere)
            RemasterSpells.ReplaceLegacySpell(SpellId.FlamingSphere, "FloatingFlame", 2, (spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                return CreateFloatingFlameSpell(spellLevel);
            });

            // Laughing Fit (formerly Hideous Laughter)
            RemasterSpells.ReplaceLegacySpell(SpellId.HideousLaughter, "LaughingFit", 2, (spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                return Spells.CreateModern(IllustrationName.HideousLaughter, "Laughing Fit", new[] { Trait.Concentrate, Trait.Emotion, Trait.Manipulate, Trait.Mental, Trait.Arcane, Trait.Occult, RemasterSpells.Trait.Remaster },
                    "The target is overtaken with uncontrollable laughter.",
                    "It must attempt a Will save." +
                    S.FourDegreesOfSuccess("The target is unaffected.", "The target is plagued with uncontrollable laughter. It can't use reactions.",
                        "The target is slowed 1 and can't use reactions.", "The target falls prone and can't use actions or reactions for 1 round. It then takes the effects of a failure."),
                    Target.Ranged(6).WithAdditionalConditionOnTargetCreature(new LivingCreatureTargetingRequirement()), spellLevel, SpellSavingThrow.Standard(Defense.Will))
                .WithSoundEffect(SfxName.HideousLaughterVoiceMaleB02)
                .WithEffectOnEachTarget(async (CombatAction spell, Creature caster, Creature target, CheckResult checkResult) =>
                {
                    if (checkResult <= CheckResult.Success)
                    {
                        if (checkResult == CheckResult.CriticalFailure)
                        {
                            await target.FallProne();
                        }

                        QEffect qEffect = new QEffect("Laughing Fit", "You can't take reactions.", ExpirationCondition.ExpiresAtEndOfSourcesTurn, caster, IllustrationName.HideousLaughter)
                        {
                            CannotExpireThisTurn = true,
                            StateCheck = (QEffect qLaughingEffect) =>
                            {
                                qLaughingEffect.Owner.AddQEffect(new QEffect(ExpirationCondition.Ephemeral)
                                {
                                    Id = QEffectId.CannotTakeReactions
                                });
                                if (checkResult <= CheckResult.Failure && !qLaughingEffect.Owner.HasEffect(QEffectId.Slowed))
                                {
                                    qLaughingEffect.Owner.AddQEffect(QEffect.Slowed(1).WithExpirationEphemeral());
                                }
                            }
                        };
                        if (checkResult == CheckResult.CriticalFailure)
                        {
                            target.AddQEffect(new QEffect("Laughing Fit (critical failure)", "You can't take actions.", ExpirationCondition.ExpiresAtStartOfSourcesTurn, caster, IllustrationName.HideousLaughter)
                            {
                                PreventTakingAction = (CombatAction action) => (action.ActionId == ActionId.EndTurn) ? null : "You can't take actions due to Laughing Fit."
                            });
                        }

                        target.AddQEffect(qEffect);
                        caster.AddQEffect(QEffect.Sustaining(spell, qEffect));
                    }
                });
            });

            // Mist (formerly Obscuring Mist)
            RemasterSpells.ReplaceLegacySpell(SpellId.ObscuringMist, "Mist", 2, (spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                return Spells.CreateModern(IllustrationName.ObscuringMist, "Mist", new[] { Trait.Concentrate, Trait.Manipulate, Trait.Water, Trait.Arcane, Trait.Primal, RemasterSpells.Trait.Remaster },
                    "You call forth a cloud of mist.",
                    "All creatures within the mist become concealed, and all creatures outside the mist become concealed to creatures within it. You can Dismiss the cloud." + "\n\n{i}Dismiss Mist is in Other maneuvers menu.{/i}",
                    Target.Burst(24, 4), spellLevel, null).WithSoundEffect(SfxName.GaleBlast).WithActionCost(3)
                .WithEffectOnChosenTargets(async (Creature caster, ChosenTargets targets) =>
                {
                    List<TileQEffect> effects = new List<TileQEffect>();
                    foreach (Tile tile in targets.ChosenTiles)
                    {
                        TileQEffect item = new TileQEffect(tile)
                        {
                            StateCheck = (_) => { tile.FoggyTerrain = true; },
                            Illustration = IllustrationName.Fog,
                            ExpiresAt = ExpirationCondition.Never
                        };
                        effects.Add(item);
                        tile.QEffects.Add(item);
                    }

                    caster.AddQEffect(new QEffect
                    {
                        ProvideActionIntoPossibilitySection = (QEffect effect, PossibilitySection section) => (section.PossibilitySectionId != PossibilitySectionId.OtherManeuvers) ? null : new ActionPossibility(new CombatAction(caster, IllustrationName.ObscuringMist, "Dismiss Mist", new[] { Trait.Concentrate }, "Dismiss this effect.", Target.Self())
                        .WithEffectOnSelf((_) =>
                        {
                            foreach (TileQEffect tileEffect in effects)
                            {
                                tileEffect.ExpiresAt = ExpirationCondition.Immediately;
                            }
                        }))
                    });
                });
            });

            // Noise Blast (formerly Sound Burst)
            RemasterSpells.ReplaceLegacySpell(SpellId.SoundBurst, "NoiseBlast", 2, (spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                return Spells.CreateModern(IllustrationName.SoundBurst, "Noise Blast", new[] { Trait.Concentrate, Trait.Manipulate, Trait.Sonic, Trait.Arcane, Trait.Divine, Trait.Occult, RemasterSpells.Trait.Remaster },
                    "A cacophonous noise blasts out dealing " + S.HeightenedVariable(spellLevel, 2) + "d10 sonic damage.",
                    "Each creature must attempt a Fortitude save." +
                    S.FourDegreesOfSuccess("The creature is unaffected.", "The creature takes half damage.", "The creature takes full damage is deafened for 1 round.", "The creature takes double damage and is deafened for 1 minute, and stunned 1."),
                    Target.Burst(6, 2), spellLevel, SpellSavingThrow.Basic(Defense.Fortitude))
                .WithSoundEffect(SfxName.SoundBurst)
                .WithEffectOnEachTarget(async (CombatAction spell, Creature caster, Creature target, CheckResult checkResult) =>
                {
                    await CommonSpellEffects.DealBasicDamage(spell, caster, target, checkResult, "2d10", DamageKind.Sonic);
                    if (checkResult == CheckResult.Failure)
                    {
                        target.AddQEffect(QEffect.Deafened().WithExpirationAtStartOfSourcesTurn(caster, 1));
                    }
                    if (checkResult == CheckResult.CriticalFailure)
                    {
                        target.AddQEffect(QEffect.Deafened().WithExpirationNever());
                        target.AddQEffect(QEffect.Stunned(1));
                    }
                });
            });

            // Oaken Resilience (formerly Barkskin)
            RemasterSpells.ReplaceLegacySpell(SpellId.Barkskin, "OakenResilience", 2, (spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                return Spells.CreateModern(IllustrationName.Barkskin, "Oaken Resilience", new[] { Trait.Concentrate, Trait.Manipulate, Trait.Plant, Trait.Wood, Trait.Arcane, Trait.Primal, RemasterSpells.Trait.Remaster },
                    "The target's skin becomes tough, with a consistency like bark or wood.",
                    "The target gains resistance 2 to bludgeoning and piercing damage and weakness 3 to fire. After the target takes fire damage, it can Dismiss the spell as a free action triggered by taking the damage; doing so doesn't reduce the fire damage the target was dealt.",
                    Target.AdjacentFriendOrSelf((Target tg, Creature a, Creature d) => (a == d && !a.HasEffect(QEffectId.Barkskin) && !a.HasEffect(QEffectId.EndedBarkskin)) ? 15 : int.MinValue), spellLevel, null).WithSoundEffect(SfxName.ArmorDon)
                .WithEffectOnEachTarget(async (CombatAction spell, Creature caster, Creature target, CheckResult checkResult) =>
                {
                    QEffect updatedEffect = QEffect.Barkskin();
                    updatedEffect.Name = "Oaken Resilience";
                    target.AddQEffect(updatedEffect);
                });
            });

            // Revealing Light (formerly Faerie Fire)
            RemasterSpells.RegisterNewSpell("RevealingLight", 2, (spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                return Spells.CreateModern(IllustrationName.AncientDust, "Revealing Light", new[] { Trait.Concentrate, Trait.Light, Trait.Manipulate, Trait.Arcane, Trait.Divine, Trait.Occult, Trait.Primal, RemasterSpells.Trait.Remaster },
                    "A wave of magical light washes over the area.",
                    "You choose the appearance of the light, such as colorful, heatless flames or sparkling motes. A creature affected by revealing light is dazzled. If the creature was invisible, it becomes concealed instead. If the creature was already concealed for any other reason, it is no longer concealed." +
                    S.FourDegreesOfSuccess("The target is unaffected.", "The light affects the creature for 2 rounds.", "The light affects the creature for 1 minute.", "The light affects the creature for 10 minutes."),
                    Target.Burst(24, 2), spellLevel, SpellSavingThrow.Standard(Defense.Reflex)).WithSoundEffect(SfxName.AncientDust)
                .WithEffectOnEachTarget(async (CombatAction spell, Creature caster, Creature target, CheckResult checkResult) =>
                {
                    switch (checkResult)
                    {
                        case CheckResult.CriticalSuccess:
                            break;
                        case CheckResult.Success:
                            target.AddQEffect(QEffect.Dazzled().WithExpirationAtStartOfSourcesTurn(caster, 2));
                            break;
                        case CheckResult.Failure:
                        case CheckResult.CriticalFailure:
                            target.AddQEffect(QEffect.Dazzled().WithExpirationNever());
                            break;
                    }
                    if (checkResult <= CheckResult.Success) {
                        // This isn't perfect, but it's mostly right.
                        if (target.HasEffect(QEffectId.Invisible)) {
                            QEffect invisibleEffect = target.QEffects.First((qEffect) => qEffect.Id == QEffectId.Invisible);
                            target.RemoveAllQEffects((qEffect) => qEffect.Id == QEffectId.Invisible);
                            target.AddQEffect(new QEffect("Concealed", "You are concealed. {i}(Everyone has an extra 20% miss chance against you.){/i}", invisibleEffect.ExpiresAt, caster, (Illustration)IllustrationName.Blur)
                            {
                                RoundsLeft = invisibleEffect.RoundsLeft,
                                CountsAsABuff = true,
                                Id = QEffectId.Blur
                            });
                        }
                    }
                });
            });

            // See the Unseen (formerly See Invisible)
            RemasterSpells.RegisterNewSpell("SeeTheUnseen", 2, (spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                return Spells.CreateModern(IllustrationName.Seek, "See the Unseen", new[] { Trait.Concentrate, Trait.Manipulate, RemasterSpells.Trait.Revelation, Trait.Arcane, Trait.Divine, Trait.Occult, RemasterSpells.Trait.Remaster },
                    "Your gaze pierces through illusions and finds invisible creatures and spirits.",
                    "You can see invisible creatures as though they weren't invisible, although their features are blurred, making them concealed and difficult to identify. You can also see incorporeal creatures, like ghosts, phased through an object from within 10 feet of an object's surface as blurry shapes seen through those objects. Subtler clues also grant you a +2 status bonus to checks you make to disbelieve illusions.",
                    Target.Self(), spellLevel, null).WithSoundEffect(SfxName.BitOfLuck)
                .WithEffectOnSelf((target) =>
                {
                    // I don't have any good hooks into HiddenRules.DetermineHidden, so instead, I'll just give myself Blind-Fight
                    target.AddQEffect(new QEffect("See the Unseen", "Your gaze pierces through illusions and finds invisible creatures and spirits.")
                    {
                        Id = QEffectId.BlindFight,
                        CountsAsABuff = true
                    }.WithExpirationNever());
                });
            });

            // Spiritual Armament(formerly Spiritual Weapon)
            RemasterSpells.ReplaceLegacySpell(SpellId.SpiritualWeapon, "SpiritualArmament", 2, (spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                return CreateSpiritualArmamentSpell(spellId, spellLevel);
            });

            // Stupefy (formerly Touch of Idiocy)
            RemasterSpells.ReplaceLegacySpell(SpellId.TouchOfIdiocy, "Stupefy", 2, (spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                return Spells.CreateModern(IllustrationName.TouchOfIdiocy, "Stupefy", new[] { Trait.Concentrate, Trait.Manipulate, Trait.Mental, Trait.Arcane, Trait.Occult, RemasterSpells.Trait.Remaster },
                    "You dull the target's mind, depending on its Will save.",
                    RemasterSpells.StripInitialWhitespace(S.FourDegreesOfSuccess("The target is unaffected.", "The target is stupefied 1 until the start of your next turn.", "The target is stupefied 2 for 1 minute.", "The target is stupefied 3 for 1 minute.")),
                    Target.Ranged(6), spellLevel, SpellSavingThrow.Standard(Defense.Will)).WithSoundEffect(SfxName.Mental)
                .WithEffectOnEachTarget(async (CombatAction spell, Creature caster, Creature target, CheckResult checkResult) =>
                {
                    switch (checkResult) {
                        case CheckResult.CriticalSuccess:
                            break;
                        case CheckResult.Success:
                            target.AddQEffect(QEffect.Stupefied(1).WithExpirationAtStartOfSourcesTurn(caster, 1));
                            break;
                        case CheckResult.Failure:
                            target.AddQEffect(QEffect.Stupefied(2).WithExpirationNever());
                            break;
                        case CheckResult.CriticalFailure:
                            target.AddQEffect(QEffect.Stupefied(3).WithExpirationNever());
                            break;
                    }
                });
            });

            // This is a level 1 spell, but we don't have any -1 creatures. To prevent it from being a trap, we treat it like a level 2 spell.
            RemasterSpells.RegisterNewSpell("SummonConstruct", 2, (spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                int maximumCreatureLevel = spellLevel == 1 ? -1 : 1;
                return Spells.CreateModern(IllustrationName.AnimatedStatue256, "Summon Construct", new[] { Trait.Concentrate, Trait.Manipulate, RemasterSpells.Trait.Summon, Trait.Arcane, RemasterSpells.Trait.Remaster },
                    "You summon a creature that has the construct trait.", "You summon a creature that has the construct trait and whose level is " + S.HeightenedVariable(maximumCreatureLevel, -1) + " or less to fight for you." + Core.CharacterBuilder.FeatsDb.Spellbook.Level1Spells.SummonRulesText + S.HeightenText(spellLevel, 1, inCombat, "{b}Heightened (2nd){/b} The maximum level of the summoned creature is 1."),
                    Target.RangedEmptyTileForSummoning(6), spellLevel, null).WithActionCost(3).WithSoundEffect(SfxName.Summoning)
                    .WithVariants(MonsterStatBlocks.MonsterExemplars.Where((creature) => creature.HasTrait(Trait.Construct) && creature.Level <= maximumCreatureLevel).Select((creature) => new SpellVariant(creature.Name, "Summon " + creature.Name, creature.Illustration)
                    {
                        GoodnessModifier = (ai, original) => original + (float)(creature.Level * 20)
                    }).ToArray()).WithCreateVariantDescription((_, variant) => RulesBlock.CreateCreatureDescription(MonsterStatBlocks.MonsterExemplarsByName[variant!.Id])).WithEffectOnChosenTargets(async (spell, caster, targets) => await CommonSpellEffects.SummonMonster(spell, caster, targets.ChosenTile!));
            });

            // This is a level 1 spell, but we don't have any -1 creatures. To prevent it from being a trap, we treat it like a level 2 spell.
            RemasterSpells.RegisterNewSpell("SummonPlantOrFungus", 1, (spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                int maximumCreatureLevel = spellLevel == 1 ? -1 : 1;
                return Spells.CreateModern(IllustrationName.WoodMephit256, "Summon Plant or Fungus", new[] { Trait.Concentrate, Trait.Manipulate, RemasterSpells.Trait.Summon, Trait.Primal, RemasterSpells.Trait.Remaster },
                    "You summon a creature that has the plant or fungus trait.", "You summon a creature that has the plant or fungus trait and whose level is " + S.HeightenedVariable(maximumCreatureLevel, -1) + " or less to fight for you." + Core.CharacterBuilder.FeatsDb.Spellbook.Level1Spells.SummonRulesText + S.HeightenText(spellLevel, 1, inCombat, "{b}Heightened (2nd){/b} The maximum level of the summoned creature is 1."),
                    Target.RangedEmptyTileForSummoning(6), spellLevel, null).WithActionCost(3).WithSoundEffect(SfxName.Summoning)
                    .WithVariants(MonsterStatBlocks.MonsterExemplars.Where((creature) => creature.HasTrait(Trait.Plant) && creature.Level <= maximumCreatureLevel).Select((creature) => new SpellVariant(creature.Name, "Summon " + creature.Name, creature.Illustration)
                    {
                        GoodnessModifier = (ai, original) => original + (float)(creature.Level * 20)
                    }).ToArray()).WithCreateVariantDescription((_, variant) => RulesBlock.CreateCreatureDescription(MonsterStatBlocks.MonsterExemplarsByName[variant!.Id])).WithEffectOnChosenTargets(async (spell, caster, targets) => await CommonSpellEffects.SummonMonster(spell, caster, targets.ChosenTile!));
            });

        }

        private static Creature CreateIllusoryObject(IllustrationName illustration, string name)
        {
            List<Trait> traits = new List<Trait>() { Trait.IllusoryObject };
            Defenses defenses = new Defenses(0, 0, 0, 0);
            Abilities abilities = new Abilities(0, 0, 0, 0, 0, 0);
            Skills skills = new Skills();
            return new Creature(illustration, name, traits, 1, 0, 10, defenses, 1, abilities, skills).AddQEffect(QEffect.Flying());
        }

        // Targets an accessible and available tile within a specified distance.
        private static TileTarget RangedTileTarget(int distance)
        {
            return new TileTarget((Creature caster, Tile tile) =>
            {
                if (!tile.AlwaysBlocksMovement)
                {
                    Tile occupies = caster.Occupies;
                    if (occupies != null && occupies.DistanceTo(tile) <= distance)
                    {
                        return (int)caster.Occupies.HasLineOfEffectToIgnoreLesser(tile) < 4;
                    }
                }

                return false;
            }, null);
        }

        // Applies the damage effect and temporary invulnerability to the target.
        private async static Task PerformFlamingSphereAttack(CombatAction spell, Creature caster, Creature target, string damage)
        {
            if (!target.QEffects.Any((qEffect) => qEffect.Name == "Floating Flame Immunity"))
            {
                CheckResult checkResult = CommonSpellEffects.RollSpellSavingThrow(target, spell, Defense.Reflex);
                if (checkResult != CheckResult.CriticalSuccess)
                {
                    await CommonSpellEffects.DealBasicDamage(spell, caster, target, checkResult, damage, DamageKind.Fire);
                    target.AddQEffect(new QEffect("Floating Flame Immunity", "A given creature can take damage from {i}floating flame{/i} only once per round.").WithExpirationAtStartOfSourcesTurn(caster, 1));
                }
            }
        }

        /// <summary>
        ///  Floating Flame spell
        /// </summary>
        /// <param name="spellLevel"></param>
        /// <returns></returns>
        private static CombatAction CreateFloatingFlameSpell(int spellLevel)
        {
            return Spells.CreateModern(IllustrationName.FlamingSphere, "Floating Flame", new[] { Trait.Concentrate, Trait.Fire, Trait.Manipulate, Trait.Arcane, Trait.Primal, RemasterSpells.Trait.Remaster },
                "You create a fire that burns without fuel and moves to your commands.",
                "The flame deals 3d6 fire damage to each creature in the square in which it appears, with a basic Reflex save. When you Sustain this spell, you can levitate the flame up to 10 feet. It then deals damage to each creature whose space it shared at any point during its flight. This uses the same damage and save, and you roll the damage once each time you Sustain. A given creature can take damage from floating flame only once per round.",
                RangedTileTarget(6), spellLevel, SpellSavingThrow.Basic(Defense.Reflex))
            .WithSoundEffect(SfxName.RejuvenatingFlames).WithProjectileCone((Illustration)IllustrationName.SpiritualWeapon, 0, ProjectileKind.None)
            .WithEffectOnChosenTargets(async (CombatAction spell, Creature caster, ChosenTargets targets) =>
            {
                string damage = "3d6";
                Creature spellObject = CreateIllusoryObject(IllustrationName.FlamingSphere256, "Floating Flame");
                if (targets.ChosenTile == null)
                {
                    return;
                }
                caster.Battle.SpawnIllusoryCreature(spellObject, targets.ChosenTile);
                if (targets.ChosenTile.PrimaryOccupant != null)
                {
                    await PerformFlamingSphereAttack(spell, caster, targets.ChosenTile.PrimaryOccupant, damage);
                }
                caster.AddQEffect(new QEffect("Floating Flame", "You're sustaining a floating flame which you can use to deal fire damage. You must Sustain it each turn or it will go away.", ExpirationCondition.ExpiresAtEndOfSourcesTurn, caster, IllustrationName.FlamingSphere)
                {
                    DoNotShowUpOverhead = true,
                    WhenExpires = (qEffect) =>
                    {
                        spellObject.Occupies.Overhead("expired", Color.Blue);
                        spellObject.DeathScheduledForNextStateCheck = true;
                    },
                    CannotExpireThisTurn = true,
                    ProvideContextualAction = (qEffect) =>
                    {
                        PathTarget newTarget = new PathTarget(spellObject.Occupies, 2);
                        newTarget.SetOwnerAction(new CombatAction(caster, IllustrationName.None, "Floating Flame", spell.Traits.ToArray(), "---", Target.Self()).WithActionCost(1));
                        string name = !qEffect.CannotExpireThisTurn ? "Sustain, move and damage" : "Move and damage";
                        string description = !qEffect.CannotExpireThisTurn ? "The duration of the spell continues until the end of your next turn." : "";
                        description += "Move the Floating Flame 10-feet and damage any creatures whose space it shared at any point during its movement.";
                        return new ActionPossibility(new CombatAction(qEffect.Owner, IllustrationName.FlamingSphere256, name, new[] { Trait.Concentrate, Trait.SustainASpell, Trait.Spell, Trait.AlwaysHits }, description, newTarget)
                        {
                            SpellId = SpellId.FlamingSphere,
                            SpellcastingSource = spell.SpellcastingSource
                        }.WithEffectOnChosenTargets(async (CombatAction combatAction, Creature self, ChosenTargets chosenTargets) =>
                        {
                            qEffect.CannotExpireThisTurn = true;
                            foreach (Tile tile in chosenTargets.ChosenTiles)
                            {
                                await spellObject.MoveTo(tile, spell, new MovementStyle()
                                {
                                    Shifting = true,
                                    MaximumSquares = 1000,
                                    ShortestPath = true,
                                    Insubstantial = true
                                });
                                if (tile.PrimaryOccupant != null)
                                {
                                    await PerformFlamingSphereAttack(spell, caster, tile.PrimaryOccupant, damage);
                                }
                            }
                        }).WithSpellSavingThrow(new Defense?(Defense.Reflex))).WithPossibilityGroup("Maintain an activity");
                    },
                    StateCheck = (qEffect) =>
                    {
                        if (qEffect.Owner.Actions.CanTakeActions())
                            return;
                        qEffect.ExpiresAt = ExpirationCondition.Immediately;
                    }
                });
            });
        }

        private async static Task PerformSpiritualArmamentAttack(CombatAction spell, Creature caster, Creature target, CheckResult checkResult, string diceExpression)
        {
            IEnumerable<DamageKind> damageKinds = new List<DamageKind>();
            if (caster.PrimaryItem != null && caster.PrimaryItem.HasTrait(Trait.Weapon)) {
                damageKinds = damageKinds.Union(caster.PrimaryItem.DetermineDamageKinds());
            }
            if (caster.SecondaryItem != null && caster.SecondaryItem.HasTrait(Trait.Weapon))
            {
                damageKinds = damageKinds.Union(caster.SecondaryItem.DetermineDamageKinds());
            }
            if (caster.HasTrait(Trait.Cleric))
            {
                // TODO: this should be Spirit, potentially with Holy or Unholy depending on the caster
                damageKinds = damageKinds.Union(new List<DamageKind> { DamageKind.Good });
            }
            DamageKind damageKind = target.WeaknessAndResistance.WhatDamageKindIsBestAgainstMe(damageKinds.ToArray());
            await CommonSpellEffects.DealAttackRollDamage(spell, caster, target, checkResult, diceExpression, damageKind);
        }

        /// <summary>
        ///  Spiritual Armament spell
        /// </summary>
        /// <param name="spellLevel"></param>
        /// <returns></returns>
        private static CombatAction CreateSpiritualArmamentSpell(SpellId spellId, int spellLevel)
        {
            int heightenIncrements = (spellLevel - 2) / 2;
            string damage = (2 + heightenIncrements) + "d8";
            return Spells.CreateModern(IllustrationName.SpiritualWeapon, "Spiritual Armament", new[] { Trait.Concentrate, Trait.Manipulate, RemasterSpells.Trait.Sanctified, RemasterSpells.Trait.Spirit, Trait.Divine, Trait.Occult, RemasterSpells.Trait.Remaster },
                "You create a ghostly, magical echo of one weapon you're wielding or wearing and fling it.",
                "Attempt a spell attack roll against the target's AC, dealing " + S.HeightenedVariable(2 + heightenIncrements, 2) + "d8 damage on a hit (or double damage on a critical hit). The damage type is the same as the chosen weapon (or any of its types for a versatile weapon). The attack deals spirit damage instead if that would be more detrimental to the creature (as determined by the GM). This attack uses and contributes to your multiple attack penalty. After the attack, the weapon returns to your side. If you sanctify the spell, the attacks are sanctified as well.\n\n" +
                "Each time you Sustain the spell, you can repeat the attack against any creature within 120 feet.",
                Target.Ranged(24), spellLevel, null)
            .WithSoundEffect(SfxName.RejuvenatingFlames).WithProjectileCone((Illustration)IllustrationName.SpiritualWeapon, 0, ProjectileKind.None).WithSpellAttackRoll()
            .WithEffectOnSelf(async (CombatAction spell, Creature caster) =>
            {
                caster.AddQEffect(new QEffect("Spiritual Armament", "You have a spiritual armament which you can use to attack. You must Sustain it each turn or it will go away.", ExpirationCondition.ExpiresAtEndOfSourcesTurn, caster, IllustrationName.SpiritualWeapon)
                {
                    DoNotShowUpOverhead = true,
                    CannotExpireThisTurn = true,
                    ProvideContextualAction = (qEffect) =>
                    {
                        Target newTarget = Target.Ranged(24);
                        newTarget.SetOwnerAction(new CombatAction(caster, IllustrationName.None, "Spiritual Armament", spell.Traits.ToArray(), "---", Target.Self()).WithActionCost(1));
                        string name = "Sustain and attack";
                        string description = !qEffect.CannotExpireThisTurn ? "The duration of the spell continues until the end of your next turn." : "";
                        description += "Attack a creature within 120 feet.";
                        return new ActionPossibility(new CombatAction(qEffect.Owner, IllustrationName.SpiritualWeapon, name, new[] { Trait.Attack, Trait.Concentrate, Trait.SustainASpell, Trait.Spell }, description, newTarget)
                        {
                            SpellId = spellId,
                            SpellcastingSource = spell.SpellcastingSource
                        }.WithSpellAttackRoll()
                        .WithEffectOnSelf((_) => { qEffect.CannotExpireThisTurn = true; })
                        .WithEffectOnEachTarget(async (CombatAction spell, Creature caster, Creature target, CheckResult checkResult) => { await PerformSpiritualArmamentAttack(spell, caster, target, checkResult, damage); })
                        ).WithPossibilityGroup("Maintain an activity");
                    },
                    StateCheck = (qEffect) =>
                    {
                        if (qEffect.Owner.Actions.CanTakeActions())
                            return;
                        qEffect.ExpiresAt = ExpirationCondition.Immediately;
                    }
                });
            })
            .WithEffectOnEachTarget(async (CombatAction spell, Creature caster, Creature target, CheckResult checkResult) => { await PerformSpiritualArmamentAttack(spell, caster, target, checkResult, damage); });
        }

        private static ActionPossibility EscapeAction(Creature creature, QEffect qEffect, SpellcastingSource source)
        {
            CombatAction combatAction = new CombatAction(creature, IllustrationName.Escape, "Escape from " + qEffect.Name, new[] { Trait.Attack, Trait.AttackDoesNotTargetAC },
                "Acrobatics check or Athletics check aganst the Spell DC of the effect restraining you.", Target.Self((_, ai) => ai.EscapeFrom(qEffect.Source ?? creature)))
            {
                ActionId = ActionId.Escape
            };
            ActiveRollSpecification activeRollSpecification = new ActiveRollSpecification(Checks.BestRoll(Checks.SkillCheck(Skill.Athletics), Checks.SkillCheck(Skill.Acrobatics)), Checks.FlatDC(source.GetSpellSaveDC()));
            return new ActionPossibility(combatAction.WithActiveRollSpecification(activeRollSpecification).WithSoundEffect(combatAction.Owner.HasTrait(Trait.Female) ? SfxName.TripFemale : SfxName.TripMale)
                .WithEffectOnEachTarget(async (spell, creature, d, checkResult) =>
                {
                    switch (checkResult)
                    {
                        case CheckResult.Success:
                            qEffect.ExpiresAt = ExpirationCondition.Immediately;
                            break;
                        case CheckResult.CriticalSuccess:
                            qEffect.ExpiresAt = ExpirationCondition.Immediately;
                            break;
                    }
                }));
        }
    }
}
