using System;
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
using Dawnsbury.Modding;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Display.Text;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Roller;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Tiles;
using System.Collections.Generic;
using System.Linq;
using Dawnsbury.Display.Illustrations;

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
        // * Entangling Flora (from Entangle)
        // * Illusory Creature
        // * Share Life
        // * Sound Body
        public static void RegisterSpells()
        {
            // Renamed from Acid Arrow. Updated traits, description, and functionality
            ModManager.RegisterNewSpell("AcidGrip", 2, ((spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
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
                .WithEffectOnEachTarget(async delegate (CombatAction spell, Creature caster, Creature target, CheckResult checkResult)
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
            }));

            // ScorchingRay => Blazing Bolts
            ModManager.RegisterNewSpell("BlazingBolt", 2, ((spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
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
                .WithEffectOnChosenTargets(async delegate (CombatAction spell, Creature caster, ChosenTargets targets)
                {
                    string damageDice = (spell.SpentActions > 1) ? (2 * spellLevel + "d6") : (spellLevel + "d6");
                    foreach (Creature target in targets.GetTargetCreatures()) 
                    {
                        await CommonSpellEffects.DealAttackRollDamage(spell, caster, target, targets.CheckResults[target], damageDice, DamageKind.Fire);
                    }
                    // Each extra ray increases our attack penalty (after all rolls)
                    caster.Actions.AttackedThisManyTimesThisTurn += spell.SpentActions - 1;
                })
                .WithTargetingTooltip(delegate (CombatAction power, Creature creature, int index)
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
            }));

            // Calm (formerly Calm Emotions)
            ModManager.RegisterNewSpell("Calm", 2, ((spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                return Spells.CreateModern(IllustrationName.CalmEmotions, "Calm", new[] { Trait.Concentrate, Trait.Emotion, Trait.Incapacitation, Trait.Manipulate, Trait.Mental, Trait.Divine, Trait.Occult, RemasterSpells.Trait.Remaster },
                    "You forcibly calm creatures in the area, soothing them into a nonviolent state; each creature must attempt a Will save.",
                    S.FourDegreesOfSuccess("The creature is unaffected.", "Calming urges impose a –1 status penalty to the creature's attack rolls.",
                        "Any emotion effects that would affect the creature are suppressed and the creature can't use hostile actions. If the target is subject to hostility from any other creature, it ceases to be affected by {i}calm{/i}.", "As failure, but hostility doesn't end the effect."),
                        Target.Burst(24, 2), spellLevel, SpellSavingThrow.Standard(Defense.Will)).WithSoundEffect(SfxName.PureEnergyRelease)
                .WithEffectOnEachTarget(async delegate (CombatAction spell, Creature caster, Creature target, CheckResult result)
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
                            qEffect.AddGrantingOfTechnical((Creature cr) => cr.EnemyOf(target), delegate (QEffect qfTechnical)
                            {
                                qfTechnical.AfterYouTakeHostileAction = delegate (QEffect qfTechnical2, CombatAction hostileAction)
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

                    List<QEffect> list = (List<QEffect>)qSustainMultipleEffect.Tag;
                    list.Add(qEffect);
                });
            }));

            // False Vitality (handle like Mystic Armor)
            // Floating Flame (formerly Flaming Sphere)

            // Laughing Fit (formerly Hideous Laughter)
            ModManager.RegisterNewSpell("LaughingFit", 2, ((spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                return Spells.CreateModern(IllustrationName.HideousLaughter, "Laughing Fit", new[] { Trait.Concentrate, Trait.Emotion, Trait.Manipulate, Trait.Mental, Trait.Arcane, Trait.Occult, RemasterSpells.Trait.Remaster },
                    "The target is overtaken with uncontrollable laughter.",
                    "It must attempt a Will save." +
                    S.FourDegreesOfSuccess("The target is unaffected.", "The target is plagued with uncontrollable laughter. It can't use reactions.",
                        "The target is slowed 1 and can't use reactions.", "The target falls prone and can't use actions or reactions for 1 round. It then takes the effects of a failure."),
                    Target.Ranged(6).WithAdditionalConditionOnTargetCreature(new LivingCreatureTargetingRequirement()), spellLevel, SpellSavingThrow.Standard(Defense.Will))
                .WithSoundEffect(SfxName.HideousLaughterVoiceMaleB02)
                .WithEffectOnEachTarget(async delegate (CombatAction spell, Creature caster, Creature target, CheckResult checkResult)
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
                            StateCheck = delegate (QEffect qLaughingEffect)
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
            }));

            // Mist (formerly Obscuring Mist)
            ModManager.RegisterNewSpell("Mist", 2, ((spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                return Spells.CreateModern(IllustrationName.ObscuringMist, "Mist", new[] { Trait.Concentrate, Trait.Manipulate, Trait.Water, Trait.Arcane, Trait.Primal, RemasterSpells.Trait.Remaster },
                    "You call forth a cloud of mist.",
                    "All creatures within the mist become concealed, and all creatures outside the mist become concealed to creatures within it. You can Dismiss the cloud." + "\n\n{i}Dismiss Mist is in Other maneuvers menu.{/i}",
                    Target.Burst(24, 4), spellLevel, null).WithSoundEffect(SfxName.GaleBlast).WithActionCost(3)
                .WithEffectOnChosenTargets(async delegate (Creature caster, ChosenTargets targets)
                {
                    List<TileQEffect> effects = new List<TileQEffect>();
                    foreach (Tile tile in targets.ChosenTiles)
                    {
                        TileQEffect item = new TileQEffect(tile)
                        {
                            StateCheck = delegate
                            {
                                tile.FoggyTerrain = true;
                            },
                            Illustration = IllustrationName.Fog,
                            ExpiresAt = ExpirationCondition.Never
                        };
                        effects.Add(item);
                        tile.QEffects.Add(item);
                    }

                    caster.AddQEffect(new QEffect
                    {
                        ProvideActionIntoPossibilitySection = (QEffect effect, PossibilitySection section) => (section.PossibilitySectionId != PossibilitySectionId.OtherManeuvers) ? null : new ActionPossibility(new CombatAction(caster, IllustrationName.ObscuringMist, "Dismiss Mist", new[] { Trait.Concentrate }, "Dismiss this effect.", Target.Self()).WithEffectOnSelf(delegate
                        {
                            foreach (TileQEffect tileEffect in effects)
                            {
                                tileEffect.ExpiresAt = ExpirationCondition.Immediately;
                            }
                        }))
                    });
                });
            }));

            // Noise Blast (formerly Sound Burst)
            ModManager.RegisterNewSpell("NoiseBlast", 2, ((spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                return Spells.CreateModern(IllustrationName.SoundBurst, "Noise Blast", new[] { Trait.Concentrate, Trait.Manipulate, Trait.Sonic, Trait.Arcane, Trait.Divine, Trait.Occult, RemasterSpells.Trait.Remaster },
                    "A cacophonous noise blasts out dealing " + S.HeightenedVariable(spellLevel, 2) + "d10 sonic damage.",
                    "Each creature must attempt a Fortitude save." + 
                    S.FourDegreesOfSuccess("The creature is unaffected.", "The creature takes half damage.", "The creature takes full damage is deafened for 1 round.", "The creature takes double damage and is deafened for 1 minute, and stunned 1."),
                    Target.Burst(6, 2), spellLevel, SpellSavingThrow.Basic(Defense.Fortitude))
                .WithSoundEffect(SfxName.SoundBurst)
                .WithEffectOnEachTarget(async delegate (CombatAction spell, Creature caster, Creature target, CheckResult checkResult)
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
            }));

            // Oaken Resilience (formerly Barkskin)
            ModManager.RegisterNewSpell("OakenResilience", 2, ((spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                return Spells.CreateModern(IllustrationName.Barkskin, "Oaken Resilience", new[] { Trait.Concentrate, Trait.Manipulate, Trait.Plant, Trait.Wood, Trait.Arcane, Trait.Primal, RemasterSpells.Trait.Remaster },
                    "The target's skin becomes tough, with a consistency like bark or wood.",
                    "The target gains resistance 2 to bludgeoning and piercing damage and weakness 3 to fire. After the target takes fire damage, it can Dismiss the spell as a free action triggered by taking the damage; doing so doesn't reduce the fire damage the target was dealt.", 
                    Target.AdjacentFriendOrSelf((Target tg, Creature a, Creature d) => (a == d && !a.HasEffect(QEffectId.Barkskin) && !a.HasEffect(QEffectId.EndedBarkskin)) ? 15 : int.MinValue), spellLevel, null).WithSoundEffect(SfxName.ArmorDon)
                .WithEffectOnEachTarget(async delegate (CombatAction spell, Creature caster, Creature target, CheckResult checkResult)
                {
                    QEffect updatedEffect = QEffect.Barkskin();
                    updatedEffect.Name = "Oaken Resilience";
                    target.AddQEffect(updatedEffect);
                });
            }));

            // Revealing Light (formerly Faerie Fire)
            // See the Unseen (formerly See Invisible)

            // Spiritual Armament (formerly Spiritual Weapon)
            //ModManager.RegisterNewSpell("SpiritualArmament", 2, ((spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            //{
            //    return Spells.CreateModern(IllustrationName.SpiritualWeapon, "Spiritual Armament", new[] { Trait.Concentrate, Trait.Manipulate, RemasterSpells.Trait.Sanctified, RemasterSpells.Trait.Spirit, Trait.Divine, Trait.Occult, RemasterSpells.Trait.Remaster },
            //        "You create a ghostly, magical echo of one weapon you're wielding or wearing and fling it.",
            //        "Attempt a spell attack roll against the target's AC, dealing 2d8 damage on a hit (or double damage on a critical hit). The damage type is the same as the chosen weapon (or any of its types for a versatile weapon). The attack deals spirit damage instead if that would be more detrimental to the creature (as determined by the GM). This attack uses and contributes to your multiple attack penalty. After the attack, the weapon returns to your side. If you sanctify the spell, the attacks are sanctified as well.",
            //        Target.Ranged(24), spellLevel, null).WithSpellAttackRoll().WithSoundEffect(SfxName.PureEnergyRelease)
            //    .WithProjectileCone(IllustrationName.SpiritualWeapon, 0, ProjectileKind.None)
            //    .WithEffectOnEachTarget(async delegate (CombatAction spell, Creature caster, Creature target, CheckResult checkResult)
            //    {
            //        Action<QEffect> attackAction = (qEffect) =>
            //        {

            //        };
            //        QEffect qEffect = new QEffect("Spiritual Armament", "Sustaining Spiritual Armament", ExpirationCondition.ExpiresAtStartOfSourcesTurn, caster, IllustrationName.SpiritualWeapon);
            //        //caster.AddQEffect(QEffect.Sustaining(spell, effect, attackAction());
            //    });
            //}));

            // Stupefy (formerly Touch of Idiocy)
            ModManager.RegisterNewSpell("Stupefy", 2, ((spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                return Spells.CreateModern(IllustrationName.TouchOfIdiocy, "Stupefy", new[] { Trait.Concentrate, Trait.Manipulate, Trait.Mental, Trait.Arcane, Trait.Occult, RemasterSpells.Trait.Remaster },
                    "You dull the target's mind, depending on its Will save.", 
                    S.FourDegreesOfSuccess("The target is unaffected.", "The target is stupefied 1 until the start of your next turn.", "The target is stupefied 2 for 1 minute.", "The target is stupefied 3 for 1 minute."),
                    Target.Ranged(6), spellLevel, SpellSavingThrow.Standard(Defense.Will)).WithSoundEffect(SfxName.Mental)
                .WithEffectOnEachTarget(async delegate (CombatAction spell, Creature caster, Creature target, CheckResult checkResult)
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
            }));
        }
    }
}
