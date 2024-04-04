using Dawnsbury.Audio;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core;
using System.Linq;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.Animations;
using Microsoft.Xna.Framework;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Modding;
using Dawnsbury.Display.Text;
using Dawnsbury.Core.Mechanics.Damage;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Roller;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace Dawnsbury.Mods.Remaster.Spellbook
{
    internal class Level1Spells
    {
        // The following spells are excluded because they aren't useful enough in gameplay
        // * Air Bubble
        // * Alarm
        // * Ant Haul
        // * Charm
        // * Cleanse Cuisine (formerly Purify Food and Drink)
        // * Create Water
        // * Disguise Magic
        public static void RegisterSpells()
        {
            ModManager.ReplaceExistingSpell(SpellId.Bless, 1, ((spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                return Bless(spellLevel, inCombat, IllustrationName.Bless, true);
            }));
            ModManager.ReplaceExistingSpell(SpellId.Bane, 1, ((spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                return Bless(spellLevel, inCombat, IllustrationName.Bane, false);
            }));

            // Renamed from Burning Hands. Updated traits and description.
            ModManager.RegisterNewSpell("BreatheFire", 1, ((spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                return Spells.CreateModern(IllustrationName.BurningHands, "Breathe Fire", new[] { Trait.Concentrate, Trait.Fire, Trait.Manipulate, Trait.Arcane, Trait.Primal, RemasterSpells.Trait.Remaster },
                    "A gout of flame sprays from your mouth.",
                    "You deal " + S.HeightenedVariable(2 * spellLevel, 2) + "d6 fire damage to creatures in the area with a basic Reflex save." +
                    S.HeightenedDamageIncrease(spellLevel, inCombat, "2d6"),
                    Target.FifteenFootCone(), spellLevel, SpellSavingThrow.Basic(Defense.Reflex)).WithSoundEffect(SfxName.Fireball).WithGoodnessAgainstEnemy((Target t, Creature a, Creature d) => (float)(t.OwnerAction.SpellLevel * 2) * 3.5f)
                .WithEffectOnEachTarget(async delegate (CombatAction spell, Creature caster, Creature target, CheckResult checkResult)
                {
                    await CommonSpellEffects.DealBasicDamage(spell, caster, target, checkResult, 2 * spellLevel + "d6", DamageKind.Fire);
                });
            }));

            // Renamed from Color Spray. Updated traits and short description.
            ModManager.RegisterNewSpell("DizzyingColors", 1, ((spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                return Spells.CreateModern(IllustrationName.ColorSpray, "Dizzying Colors", new[] { Trait.Concentrate, Trait.Illusion, Trait.Incapacitation, Trait.Manipulate, Trait.Visual, Trait.Arcane, Trait.Occult, RemasterSpells.Trait.Remaster },
                    "You unleash a swirling multitude of colors that overwhelms creatures based on their Will saves.",
                    "Each target makes a Will save.\n\n{b}Critical success{/b} The creature is unaffected.\n{b}Success{/b} The creature is dazzled for 1 round.\n{b}Failure{/b} The creature is stunned 1, blinded for 1 round, and dazzled for the rest of the encounter.\n{b}Critical failure{/b} The creature is stunned for 1 round and blinded for the rest of the encounter.",
                    Target.FifteenFootCone(), spellLevel, SpellSavingThrow.Standard(Defense.Will)).WithSoundEffect(SfxName.MagicMissile).WithProjectileCone(IllustrationName.Pixel, 25, ProjectileKind.ColorSpray)
                .WithGoodness((Target t, Creature a, Creature d) => a.AI.ColorSpray(d))
                .WithEffectOnEachTarget(async delegate (CombatAction spell, Creature caster, Creature target, CheckResult checkResult)
                {
                    switch (checkResult)
                    {
                        case CheckResult.Success:
                            target.AddQEffect(QEffect.Dazzled().WithExpirationAtStartOfSourcesTurn(caster, 1));
                            break;
                        case CheckResult.Failure:
                            target.AddQEffect(QEffect.Dazzled().WithExpirationNever());
                            target.AddQEffect(QEffect.Blinded().WithExpirationAtStartOfSourcesTurn(caster, 1));
                            target.AddQEffect(QEffect.Stunned(1));
                            break;
                        case CheckResult.CriticalFailure:
                            target.AddQEffect(QEffect.Blinded().WithExpirationNever());
                            target.AddQEffect(QEffect.Stunned(3));
                            break;
                    }
                });
            }));

            // Ray of Enfeeblement wasn't included, and this remastered version is useful.
            ModManager.RegisterNewSpell("Enfeeble", 1, ((spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                return Spells.CreateModern(IllustrationName.Enfeebled, "Enfeeble", new[] { Trait.Concentrate, Trait.Manipulate, Trait.Arcane, Trait.Divine, Trait.Occult, RemasterSpells.Trait.Remaster },
                    "You sap the target's strength, depending on its Fortitude save.",
                    S.FourDegreesOfSuccess("The target is unaffected.", "The target is enfeebled 1 until the start of your next turn.",
                                           "The target is enfeebled 2 for 1 minute.", "The target is enfeebled 3 for 1 minute."),
                    Target.Ranged(6), spellLevel, SpellSavingThrow.Standard(Defense.Fortitude)).WithSoundEffect(SfxName.Necromancy)
                .WithEffectOnEachTarget(async delegate (CombatAction spell, Creature caster, Creature target, CheckResult checkResult)
                {
                    switch (checkResult)
                    {
                        case CheckResult.Success:
                            target.AddQEffect(QEffect.Enfeebled(1).WithExpirationAtStartOfSourcesTurn(caster, 1));
                            break;
                        case CheckResult.Failure:
                            target.AddQEffect(QEffect.Enfeebled(2).WithExpirationNever());
                            break;
                        case CheckResult.CriticalFailure:
                            target.AddQEffect(QEffect.Enfeebled(3).WithExpirationNever());
                            break;
                    }
                });
            }));

            // Renamed from Magic Missile. Updated traits and short description.
            ModManager.RegisterNewSpell("ForceBarrage", 1, ((spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                Func<CreatureTarget> func = () => Target.Ranged(24, (Target tg, Creature attacker, Creature defender) => attacker.AI.DealDamage(defender, 3.5f, tg.OwnerAction));
                return Spells.CreateModern(IllustrationName.MagicMissile, "Force Barrage", new[] { Trait.Concentrate, Trait.Force, Trait.Manipulate, Trait.Arcane, Trait.Occult, RemasterSpells.Trait.Remaster },
                    "You fire a shard of solidified magic toward a creature that you can see",
                    "{b}Range{/b} 120 feet\n{b}Targets{/b} 1, 2 or 3 creatures\n\nYou send up to three darts of force. They each automatically hit and deal 1d4+1 force damage. {i}(All darts against a single target count as a single damage event.){/i}\n\nYou can spend 1–3 actions on this spell:\n{icon:Action} You send out 1 dart.\n{icon:TwoActions}You send out 2 darts.\n{icon:ThreeActions}You send out 3 darts.{/i}", 
                    Target.DependsOnActionsSpent(Target.MultipleCreatureTargets(func()).WithOverriddenTargetLine("1 creature", plural: false), Target.MultipleCreatureTargets(func(), func()).WithOverriddenTargetLine("1 or 2 creatures", plural: true), Target.MultipleCreatureTargets(func(), func(), func()).WithOverriddenTargetLine("1, 2 or 3 creatures", plural: true)), spellLevel, null).WithActionCost(-1).WithSoundEffect(SfxName.MagicMissile)
                .WithProjectileCone(IllustrationName.MagicMissile, 15, ProjectileKind.Ray)
                .WithCreateVariantDescription((int actionCost, SpellVariant? variant) => (actionCost != 1) ? ("You send out " + actionCost + " darts of force. They each automatically hit and deal 1d4+1 force damage. {i}(All darts against a single target count as a single damage event.)") : "You send out 1 dart of force. It automatically hits and deals 1d4+1 force damage.")
                .WithEffectOnChosenTargets(async delegate (CombatAction action, Creature caster, ChosenTargets targets)
                {
                    List<Task> list = new List<Task>();
                    foreach (Creature chosenCreature in targets.ChosenCreatures)
                    {
                        list.Add(caster.Battle.SpawnOverairProjectileParticlesAsync(10, caster.Occupies, chosenCreature.Occupies, Color.White, IllustrationName.MagicMissile));
                    }

                    await Task.WhenAll(list);
                    Dictionary<Creature, int> dictionary = new Dictionary<Creature, int>();
                    foreach (Creature chosenCreature2 in targets.ChosenCreatures)
                    {
                        if (!dictionary.TryAdd(chosenCreature2, 1))
                        {
                            dictionary[chosenCreature2]++;
                        }
                    }

                    foreach (KeyValuePair<Creature, int> item4 in dictionary)
                    {
                        List<DiceFormula> list2 = new List<DiceFormula>();
                        for (int i = 0; i < item4.Value; i++)
                        {
                            list2.Add(DiceFormula.FromText("1d4+1", "Magic missile"));
                        }

                        await caster.DealDirectDamage(new DamageEvent(action, item4.Key, CheckResult.Success, list2.Select((DiceFormula formula) => new KindedDamage(formula, DamageKind.Force)).ToArray()));
                    }
                })
                .WithTargetingTooltip(delegate (CombatAction power, Creature creature, int index)
                {
                    string text7 = index switch
                    {
                        0 => "first",
                        1 => "second",
                        2 => "third",
                        _ => index + "th",
                    };
                    return "Send the " + text7 + " magic missile at " + creature?.ToString() + ". (" + (index + 1) + "/" + power.SpentActions + ")";
                });
            }));
        }

        public static CombatAction Bless(int level, bool inCombat, IllustrationName illustration, bool isBless)
        {
            return Spells.CreateModern(illustration, isBless ? "Bless" : "Bane", new[] { Trait.Aura, Trait.Concentrate, Trait.Manipulate, Trait.Mental, Trait.Divine, Trait.Occult, RemasterSpells.Trait.Remaster },
                isBless ? "Blessings from beyond help your companions strike true." :
                    "You fill the minds of your enemies with doubt.",
                isBless ? "You and your allies gain a +1 status bonus to attack rolls while within the emanation. Once per round on subsequent turns, you can Sustain the spell to increase the emanation's radius by 10 feet. Bless can counteract bane." :
                    "Enemies in the area must succeed at a Will save or take a –1 status penalty to attack rolls as long as they are in the area. Once per round on subsequent turns, you can Sustain the spell to increase the emanation's radius by 10 feet and force enemies in the area that weren't yet affected to attempt another saving throw. Bane can counteract bless.",            
                Target.Self(), level, null).WithSoundEffect(isBless ? SfxName.Bless : SfxName.Fear).WithEffectOnSelf(async delegate (CombatAction action, Creature self)
            {
                CombatAction action2 = action;
                int initialRadius = isBless ? 3 : 2;
                AuraAnimation auraAnimation = self.AnimationData.AddAuraAnimation(isBless ? IllustrationName.BlessCircle : IllustrationName.BaneCircle, initialRadius);
                QEffect qEffect = new QEffect(isBless ? "Bless" : "Bane", "[this condition has no description]", ExpirationCondition.Never, self, IllustrationName.None)
                {
                    WhenExpires = delegate
                    {
                        auraAnimation.MoveTo(0f);
                    },
                    Tag = (initialRadius, true),
                    StartOfYourTurn = async delegate (QEffect qfBless, Creature _)
                    {
                        if (qfBless?.Tag != null)
                        {
                            qfBless.Tag = ((((int, bool))qfBless.Tag).Item1, false); 
                        }
                            
                    },
                    ProvideContextualAction = delegate (QEffect qfBless)
                    {
                        if (qfBless?.Tag != null)
                        {
                            (int, bool) tag = ((int, bool))qfBless.Tag;
                            return (!tag.Item2) ? new ActionPossibility(new CombatAction(qfBless.Owner, illustration, isBless ? "Increase Bless radius" : "Increase Bane radius", new Trait[1] { Trait.Concentrate }, "Increase the radius of the " + (isBless ? "bless" : "bane") + " emanation by 5 feet.", Target.Self()).WithEffectOnSelf(delegate
                            {
                                int newEmanationSize = tag.Item1 + 2;
                                qfBless.Tag = (newEmanationSize, true);
                                auraAnimation.MoveTo(newEmanationSize);
                                if (!isBless)
                                {
                                    foreach (Creature item in qfBless.Owner.Battle.AllCreatures.Where((Creature cr) => cr.DistanceTo(qfBless.Owner) <= newEmanationSize && cr.EnemyOf(qfBless.Owner)))
                                    {
                                        item.RemoveAllQEffects((QEffect qf) => qf.Id == QEffectId.RolledAgainstBane && qf.Tag == qfBless);
                                    }
                                }
                            })).WithPossibilityGroup("Maintain an activity") : null;
                        }
                        else
                        {
                            return null;
                        }
                    }
                };
                if (isBless)
                {
                    auraAnimation.Color = Color.Yellow;
                    qEffect.StateCheck = delegate (QEffect qfBless)
                    {
                        if (qfBless?.Tag != null)
                        {
                            int emanationSize2 = (((int, bool))qfBless.Tag).Item1;
                            foreach (Creature item2 in qfBless.Owner.Battle.AllCreatures.Where((Creature cr) => cr.DistanceTo(qfBless.Owner) <= emanationSize2 && cr.FriendOf(qfBless.Owner) && !cr.HasTrait(Trait.Mindless) && !cr.HasTrait(Trait.Object)))
                            {
                                item2.AddQEffect(new QEffect("Bless", "You gain a +1 status bonus to attack rolls.", ExpirationCondition.Ephemeral, qfBless.Owner, IllustrationName.Bless)
                                {
                                    CountsAsABuff = true,
                                    BonusToAttackRolls = (QEffect qfBlessed, CombatAction attack, Creature? de) => attack.HasTrait(Trait.Attack) ? new Bonus(1, BonusType.Status, "bless") : null
                                });
                            }
                        }
                    };
                }
                else
                {
                    qEffect.StateCheckWithVisibleChanges = async delegate (QEffect qfBane)
                    {
                        if (qfBane?.Tag != null)
                        {

                            int emanationSize = (((int, bool))qfBane.Tag).Item1;
                            foreach (Creature item3 in qfBane.Owner.Battle.AllCreatures.Where((Creature cr) => cr.DistanceTo(qfBane.Owner) <= emanationSize && cr.EnemyOf(qfBane.Owner) && !cr.HasTrait(Trait.Mindless) && !cr.HasTrait(Trait.Object)))
                            {
                                if (!item3.QEffects.Any((QEffect qf) => qf.ImmuneToTrait == Trait.Mental))
                                {
                                    if (item3.QEffects.Any((QEffect qf) => qf.Id == QEffectId.FailedAgainstBane && qf.Tag == qfBane))
                                    {
                                        item3.AddQEffect(new QEffect("Bane", "You take a -1 status penalty to attack rolls.", ExpirationCondition.Ephemeral, qfBane.Owner, IllustrationName.Bane)
                                        {
                                            Key = "BanePenalty",
                                            BonusToAttackRolls = (QEffect qfBlessed, CombatAction attack, Creature? de) => attack.HasTrait(Trait.Attack) ? new Bonus(-1, BonusType.Status, "bane") : null
                                        });
                                    }
                                    else if (!item3.QEffects.Any((QEffect qf) => qf.Id == QEffectId.RolledAgainstBane && qf.Tag == qfBane))
                                    {
                                        CheckResult checkResult = CommonSpellEffects.RollSpellSavingThrow(item3, action2, Defense.Will);
                                        item3.AddQEffect(new QEffect(ExpirationCondition.Never)
                                        {
                                            Id = QEffectId.RolledAgainstBane,
                                            Tag = qfBane
                                        });
                                        if (checkResult <= CheckResult.Failure)
                                        {
                                            item3.AddQEffect(new QEffect(ExpirationCondition.Never)
                                            {
                                                Id = QEffectId.FailedAgainstBane,
                                                Tag = qfBane
                                            });
                                        }
                                    }
                                }
                            }
                        }
                    };
                }

                self.AddQEffect(qEffect);
            });
        }
    }
}
