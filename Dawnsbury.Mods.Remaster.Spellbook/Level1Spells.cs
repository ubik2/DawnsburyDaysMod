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

namespace Dawnsbury.Mods.Remaster.Spellbook
{
    internal class Level1Spells
    {
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
