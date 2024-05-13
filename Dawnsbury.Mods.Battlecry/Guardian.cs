using Dawnsbury.Core.CharacterBuilder.AbilityScores;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics.Damage;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Roller;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display.Text;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Audio;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;

namespace Dawnsbury.Mods.Battlecry
{
    internal class Guardian
    {
        // Taunt - bonus/pentaly only applies to attack rolls; doesn't change the AI behavior
        // Guardian's Armor - can't do the chain specialization
        // Ferocious Vengeance - probably skip
        // Mitigate Harm

        // Bodyguard - TODO
        // Larger Than Life - not useful in game
        // Long-Distance Taunt
        // Reactive Shield - already available
        // Shoulder Check - TODO
        // Unkind Shove

        // Armor Break - game doesn't really deal with broken state (and this ability is bad)
        // Covering Stance - a lot of functionality, but investigate
        // Hampering Sweeps - TODO
        // Raise Haft - TODO
        // Shielding Taunt - TODO

        // Area Cover
        // Averting Shield - not useful in game
        // Flying Tackle
        // Intercept Energy
        // Intercept Foe - do we get a call when an ally is targeted?
        // Shielded Attrition - probably too tricky

        // Tough to Kill - daily benefit of dying 3 to dying 2 isn't implemented
        public static IEnumerable<Feat> LoadAll()
        {
            yield return new ClassSelectionFeat(BattlecryMod.FeatName.Guardian,
                "You are the shield, the steel wall that holds back the tide of deadly force exhibited by enemies great and small. You are clad in armor that you wear like a second skin. You can angle your armor to protect yourself and your allies from damage and keep foes at bay. You also make yourself a more tempting target to take the hits that might have otherwise struck down your companions.",
                BattlecryMod.Trait.Guardian, new EnforcedAbilityBoost(Ability.Strength), 10,
                new[] { Trait.Perception, Trait.Reflex, Trait.Athletics, Trait.Simple, Trait.Martial, Trait.Unarmed, Trait.Armor, Trait.UnarmoredDefense, BattlecryMod.Trait.Guardian },
                new[] { Trait.Fortitude, Trait.Will },
                3,
                "{b}1. Guardian's Armor{/b} Even when you are struck, your armor protects you from some harm. You gain the armor specialization effects of medium and heavy armor.\n\n" +
                "{b}2. Intercept Strike {icon:Reaction}{/b} You keep your charges safe from harm, even if it means you get hurt yourself. You gain the Intercept Strike reaction.\n\n" +
                "{b}3. Shield Block {icon:Reaction}{/b} You gain the Shield Block general feat.\n\n" +
                "{b}4. Taunt{/b} Often, the best way to protect your allies is to have the enemy want to attack you instead. You gain the Taunt action.\n\n",
                null)
                .WithOnSheet((CalculatedCharacterSheetValues sheet) =>
                {
                    sheet.GrantFeat(BattlecryMod.FeatName.GuardiansArmor);
                    sheet.GrantFeat(BattlecryMod.FeatName.InterceptStrike);
                    sheet.GrantFeat(FeatName.ShieldBlock);
                    sheet.GrantFeat(BattlecryMod.FeatName.Taunt);
                    sheet.AddSelectionOption(new SingleFeatSelectionOption("GuardianFeat1", "Guardian feat", 1, (feat) => feat.HasTrait(BattlecryMod.Trait.Guardian)));
                    // FIXME: How to deal with dying2 once per day
                    // TODO: investigate what happens if they already have diehard
                    sheet.AddAtLevel(3, (sheet) => sheet.GrantFeat(FeatName.Diehard));
                });

            // I'm not dealing with the chain specialization, which provides resistance to critical hits, since the system doesn't really expose that.
            yield return new Feat(BattlecryMod.FeatName.GuardiansArmor, "Even when you are struck, your armor protects you from some harm.",
                "You gain the armor specialization effects of medium and heavy armor. In addition, you can rest normally while wearing medium and heavy armor.",
                new[] { BattlecryMod.Trait.Guardian }.ToList(), null)
                .WithOnCreature((creature) =>
                {
                    // I'm not sure why, but the creature.BaseArmor is null. Get it from the character sheet instead
                    Item? armor = creature.PersistentCharacterSheet?.InventoryForView.Armor;
                    if (armor != null)
                    {
                        // None of the armor has potency runes yet (armor.ArmorProperties.ItemBonus), so we don't add that
                        int resistance = armor.HasTrait(Trait.HeavyArmor) ? 2 : (armor.HasTrait(Trait.MediumArmor) ? 1 : 0);
                        if (resistance > 0)
                        {
                            DamageKind damageKind = armor.HasTrait(Trait.Leather) ? DamageKind.Bludgeoning : (armor.HasTrait(Trait.Composite) ? DamageKind.Piercing : (armor.HasTrait(Trait.Plate) ? DamageKind.Slashing : DamageKind.Untyped));
                            if (damageKind != DamageKind.Untyped)
                            {
                                creature.AddQEffect(QEffect.DamageResistance(damageKind, resistance));
                            }
                        }
                    }
                });

            yield return new Feat(BattlecryMod.FeatName.InterceptStrike, "You fling yourself in the way of oncoming harm to protect an ally.",
                "You take the damage instead of your ally, though thanks to your armor, you gain resistance to all damage against the triggering damage equal to 2 + your level.",
                new[] { BattlecryMod.Trait.Guardian }.ToList(), null)
                .WithPermanentQEffect("You fling yourself in the way of oncoming harm to protect an ally.", (QEffect qEffect) =>
                {
                    // At the beginning of combat, we'll apply an effect to all creatures of our faction that can potentially trigger this intercept strike action.
                    qEffect.StartOfCombat = async (QEffect qEffect) =>
                    {
                        qEffect.Owner.Battle.AllCreatures.ForEach((creature) =>
                        {
                            // Check to see if this is a valid ally
                            if (creature == qEffect.Owner || !(creature.OwningFaction.IsGaiaFriends || creature.OwningFaction.IsHumanControlled))
                            {
                                return;
                            }
                            creature.AddQEffect(new QEffect("InterceptStrikeCandidate", "An ally stands ready to intercept strikes.")
                            {
                                // Store the guardian in the QEffect Tag
                                Tag = qEffect.Owner,
                                ExpiresAt = ExpirationCondition.Never,
                                YouAreDealtDamage = async (qEffect, attacker, damageStuff, defender) =>
                                {
                                    Creature? protector = qEffect.Tag as Creature ?? throw new NullReferenceException("Intercept Strike creature is null.");
                                    if (!protector.IsAdjacentTo(defender) || !protector.Actions.CanTakeReaction())
                                    {
                                        return null;
                                    }

                                    if (!(damageStuff.Kind.IsPhysical() || qEffect.Owner.HasFeat(BattlecryMod.FeatName.InterceptEnergy) && IsEnergy(damageStuff.Kind)))
                                    {
                                        return null;
                                    }

                                    if (!await protector.Battle.AskToUseReaction(protector, defender?.ToString() + " " + "would be dealt damage by " + damageStuff.Power?.Name + ".\nUse your intercept strike to take the damage?"))
                                    {
                                        return null;
                                    }

                                    // TODO: Some bookkeeping around DealtDamageToAnotherCreature and CreaturesThatDamagedMe
                                    // NOTE: Our DamageStuff has already had any resistances and weaknesses from the initial target applied, but now we're applying that to the guardian instead
                                    // TODO: Check with 2 guardians that you don't have something wonky here
                                    int protectorDamage = damageStuff.Amount - (protector.Level + 2);
                                    // We only take damage if the amount is greater than zero (no healing and no minimum 1 for 0 damage).
                                    if (protectorDamage > 0)
                                    {
                                        await protector.DealDirectDamage(new DamageEvent(damageStuff.Power, protector, CheckResult.Success, new[] { new KindedDamage(DiceFormula.FromText(protectorDamage.ToString()), damageStuff.Kind) }));
                                    }
                                    return new SetToTargetNumberModification(0, "An ally used Intercept Strike to take the damage instead of you.");
                                }
                            });
                        });
                    };
                });

            yield return new Feat(BattlecryMod.FeatName.Taunt, "With an attention-getting gesture, a cutting remark, or a threatening shout, you get an enemy to focus their ire on you.",
                "Even mindless creatures are drawn to your taunts. Choose a creature within 30 feet, who must attempts a Will save against your class DC. Regardless of the result, it is immune to your Taunt until the beginning of your next turn. If you gesture, this action gains the visual trait. If you speak or otherwise make noise, this action gains the auditory trait. Your Taunt must have one of those two traits." +
                S.FourDegreesOfSuccess("The creature is unaffected.",
                    "Until the beginning of your next turn, the creature gains a +2 circumstance bonus to attack rolls it makes against you and to its DCs of effects that target you (for area effects, the DC increases only for you), but takes a –1 circumstance penalty to attack rolls and DCs when taking a hostile action that doesn't include you as a target.",
                    "As success, but the penalty is -2.",
                    "As success, but the penalty is -3."),
                new[] { Trait.Concentrate, BattlecryMod.Trait.Guardian }.ToList(), null)
                .WithPermanentQEffect("You get an enemy to focus their ire on you.", (QEffect qEffect) => qEffect.ProvideActionIntoPossibilitySection = (qfTaunt, section) =>
                {
                    if (section.PossibilitySectionId != PossibilitySectionId.OtherManeuvers)
                    {
                        return null;
                    }
                    CombatAction taunt = Taunt(qEffect.Owner);
                    return new ActionPossibility(taunt);
                });


            // This is just a marker feat which we check for in the taunt code
            yield return new TrueFeat(BattlecryMod.FeatName.LongDistanceTaunt, 1, "You can draw the wrath of your foes even at a great distance.",
                "When you use Taunt, you can choose a target within 120 feet.",
                new[] { BattlecryMod.Trait.Guardian }, null);


            // It's unclear from the rules, but I decided to leave Shove as an option as well, for when you don't want to deal damage.
            yield return new TrueFeat(BattlecryMod.FeatName.UnkindShove, 1, "When you push a foe away, you put the entire force of your armored form into it.",
                "When you successfully Shove a creature, that creature takes an amount of bludgeoning damage equal to your Strength modifier (double that amount on a critical success).",
                new[] { BattlecryMod.Trait.Guardian }, null)
                .WithPermanentQEffect("When you push a foe away, you put the entire force of your armored form into it.", (qEffect) => qEffect.ProvideActionIntoPossibilitySection = (qfUnkindShove, section) =>
                {
                    if (section.PossibilitySectionId != PossibilitySectionId.AttackManeuvers)
                    {
                        return null;
                    }
                    CombatAction unkindShove = Possibilities.CreateShove(qEffect.Owner);
                    int damage = Math.Max(0, qEffect.Owner.Abilities.Strength);
                    unkindShove.Name = "Unkind Shove";
                    unkindShove.Description = "You have at least one hand free. The target can't be more than one size larger than you. You push a creature away from you. Attempt an Athletics check against your target's Fortitude DC." +
                        S.FourDegreesOfSuccess("You push your target up to 10 feet away from you {Blue}and deal " + (2 * damage).ToString() + " bludgeoning damage{/Blue}. You can Stride after it, but you must move the same distance and in the same direction.",
                            "You push your target back 5 feet {Blue}and deal " + damage.ToString() + " bludgeoning damage{/Blue}. You can Stride after it, but you must move the same distance and in the same direction.",
                            null,
                            "You lose your balance, fall, and land prone.");
                    Delegates.EffectOnEachTarget shoveDamageDelegate = async (CombatAction action, Creature caster, Creature target, CheckResult checkResult) =>
                    {
                        if (checkResult >= CheckResult.Success && caster.Abilities.Strength >= 0)
                        {
                            await caster.DealDirectDamage(new DamageEvent(action, target, checkResult,
                                new[] { new KindedDamage(DiceFormula.FromText(caster.Abilities.Strength.ToString()), DamageKind.Bludgeoning) }, checkResult == CheckResult.CriticalSuccess));
                        }
                    };
                    unkindShove.WithEffectOnEachTarget((Delegates.EffectOnEachTarget)Delegate.Combine(shoveDamageDelegate, unkindShove.EffectOnOneTarget));

                    return new ActionPossibility(unkindShove);
                });


            // It's unclear from the rules, but I decided to leave Shove as an option as well, for when you don't want to deal damage.
            yield return new TrueFeat(BattlecryMod.FeatName.ShieldedTaunt, 2, "By banging loudly on your shield, you get the attention of even the most stubborn of foes.",
                "Raise a Shield and then Taunt a creature. Your Taunt gains the auditory trait, and the target takes a –1 circumstance penalty to their save.",
                new[] { Trait.Flourish, BattlecryMod.Trait.Guardian }, null)
                .WithPermanentQEffect("You get an enemy to focus their ire on you.", (QEffect qEffect) => qEffect.ProvideActionIntoPossibilitySection = (qfTaunt, section) =>
                {
                    Creature owner = qEffect.Owner;
                    if (section.PossibilitySectionId != PossibilitySectionId.ItemActions)
                    {
                        return null;
                    }
                    Item? shield = qEffect.Owner.HeldItems.FirstOrDefault((item) => item.HasTrait(Trait.Shield));
                    if (shield == null)
                    {
                        return null;
                    }
                    bool hasShieldBlock = owner.HasEffect(QEffectId.ShieldBlock) || shield.HasTrait(Trait.AlwaysOfferShieldBlock);
                    CombatAction shieldedTaunt = Taunt(owner).WithNoSaveFor((action, creature) => true).WithEffectOnEachTarget(async delegate (CombatAction action, Creature caster, Creature target, CheckResult _)
                    {

                        target.AddQEffect(new QEffect("GuardianTauntImmunity", "Immune to taunt").WithExpirationAtStartOfSourcesTurn(caster, 1));
                        target.AddQEffect(new QEffect("ShieldedTauntBonus", "bonus from shielded taunt")
                        {
                            BonusToDefenses = (effect, action, defense) => (action?.Name == "Taunt" || action?.Name == "Shielded Taunt") ? new Bonus(-1, BonusType.Circumstance, "Shielded Taunt", false) : null
                        }.WithExpirationEphemeral());
                        CheckResult checkResult = CommonSpellEffects.RollSavingThrow(target, action, Defense.Will, (_) => GetClassDC(action.Owner));
                        if (checkResult == CheckResult.CriticalSuccess)
                        {
                            return;
                        }
                        int penalty = checkResult switch { CheckResult.Success => -1, CheckResult.Failure => -2, CheckResult.CriticalFailure => -3, _ => 0 };
                        target.AddQEffect(new QEffect("GuardianTaunt", "Taunted")
                        {
                            BonusToAttackRolls = (qEffect, action, defender) =>
                            {
                                if (defender == caster)
                                {
                                    return new Bonus(2, BonusType.Circumstance, "Taunt", true);
                                }
                                else if (!action.Targets(caster) && penalty < 0)
                                {
                                    return new Bonus(penalty, BonusType.Circumstance, "Taunt", false);
                                }
                                return null;
                            }
                        }.WithExpirationAtStartOfSourcesTurn(caster, 1));

                    }).WithEffectOnSelf((caster) =>
                    {
                        QEffect qShieldRaised = QEffect.RaisingAShield(hasShieldBlock);
                        if (hasShieldBlock)
                            qShieldRaised.YouAreDealtDamage = async (qEffect, attacker, damageStuff, defender) =>
                            {
                                if (damageStuff.Kind == DamageKind.Bludgeoning || damageStuff.Kind == DamageKind.Piercing || damageStuff.Kind == DamageKind.Slashing)
                                {
                                    int preventHowMuch = Math.Min(shield.Hardness, damageStuff.Amount);
                                    if (await defender.Battle.AskToUseReaction(defender, "You are about to be dealt damage by " + damageStuff.Power?.Name + ".\nUse Shield Block to resist " + preventHowMuch.ToString() + " damage?"))
                                    {
                                        qShieldRaised.YouAreDealtDamage = null;
                                        return new ReduceDamageModification(preventHowMuch, "Shield block");
                                    }
                                }
                                return null;
                            };
                        caster.AddQEffect(qShieldRaised);
                    });
                    shieldedTaunt.Name = "Shielded Taunt";
                    shieldedTaunt.Illustration = shield.Illustration;
                    shieldedTaunt.Traits.AddRange(new[] { Trait.Flourish, Trait.Auditory });
                    shieldedTaunt.Description = "Raise a Shield and then Taunt a creature. Your Taunt gains the auditory trait, and the target takes a –1 circumstance penalty to their save.";
                    return new ActionPossibility(shieldedTaunt);
                });

            // This is just a marker feat which we check for in the intercept-strike code
            yield return new TrueFeat(BattlecryMod.FeatName.InterceptEnergy, 4, "By tempering your armor with chemicals, you can use it to absorb energy damage.",
                "Your Intercept Strike also triggers when an adjacent ally would take acid, cold, fire, electricity, or sonic damage.",
                new[] { BattlecryMod.Trait.Guardian }, null);
        }

        public static int GetClassDC(Creature? caster)
        {
            return 10 + (caster != null ? caster.Abilities.Intelligence : 0) + (caster != null ? caster.Proficiencies.Get(BattlecryMod.Trait.Guardian).ToNumber(caster.Level) : 0);
        }

        private static CombatAction Taunt(Creature owner)
        {
            int tauntRange = owner.HasFeat(BattlecryMod.FeatName.LongDistanceTaunt) ? 24 : 6;
            CombatAction taunt = new CombatAction(owner, IllustrationName.GenericCombatManeuver, "Taunt", new[] { Trait.Concentrate, BattlecryMod.Trait.Guardian },
                "With an attention-getting gesture, a cutting remark, or a threatening shout, you get an enemy to focus their ire on you." +
                "Even mindless creatures are drawn to your taunts. Choose a creature within 30 feet, who must attempts a Will save against your class DC. Regardless of the result, it is immune to your Taunt until the beginning of your next turn. If you gesture, this action gains the visual trait. If you speak or otherwise make noise, this action gains the auditory trait. Your Taunt must have one of those two traits." +
                S.FourDegreesOfSuccess("The creature is unaffected.",
                    "Until the beginning of your next turn, the creature gains a +2 circumstance bonus to attack rolls it makes against you and to its DCs of effects that target you (for area effects, the DC increases only for you), but takes a –1 circumstance penalty to attack rolls and DCs when taking a hostile action that doesn't include you as a target.",
                    "As success, but the penalty is -2.",
                    "As success, but the penalty is -3."),
                Target.RangedCreature(tauntRange).WithAdditionalConditionOnTargetCreature((c, t) => t.QEffects.Any((qf) => qf.Name == "GuardianTauntImmunity" && qf.Source == c) ? Usability.NotUsableOnThisCreature("Target is immune to your taunt until the beginning of your next turn.") : Usability.Usable))
            .WithActionCost(1)
            .WithSoundEffect(SfxName.BeastRoar)
            .WithSavingThrow(new SavingThrow(Defense.Will, (Creature? cr) => GetClassDC(cr)))
            .WithEffectOnEachTarget(async delegate (CombatAction action, Creature caster, Creature target, CheckResult checkResult)
            {
                target.AddQEffect(new QEffect("GuardianTauntImmunity", "Immune to taunt").WithExpirationAtStartOfSourcesTurn(caster, 1));
                if (checkResult == CheckResult.CriticalSuccess)
                {
                    return;
                }
                int penalty = checkResult switch { CheckResult.Success => -1, CheckResult.Failure => -2, CheckResult.CriticalFailure => -3, _ => 0 };
                target.AddQEffect(new QEffect("GuardianTaunt", "Taunted")
                {
                    BonusToAttackRolls = (qEffect, action, defender) =>
                    {
                        if (defender == caster)
                        {
                            return new Bonus(2, BonusType.Circumstance, "Taunt", true);
                        }
                        else if (!action.Targets(caster) && penalty < 0)
                        {
                            return new Bonus(penalty, BonusType.Circumstance, "Taunt", false);
                        }
                        return null;
                    }
                }.WithExpirationAtStartOfSourcesTurn(caster, 1));
            });

            return taunt;
        }

        private static bool IsEnergy(DamageKind damageKind)
        {
            return damageKind == DamageKind.Acid || damageKind == DamageKind.Cold || damageKind == DamageKind.Fire || damageKind == DamageKind.Electricity || damageKind == DamageKind.Sonic;
        }
    }
}
