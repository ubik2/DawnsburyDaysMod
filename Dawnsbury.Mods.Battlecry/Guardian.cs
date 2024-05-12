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

namespace Dawnsbury.Mods.Battlecry
{
    internal class Guardian
    {
        // Taunt
        // Ferocious Vengeance - probably skip
        // Mitigate Harm

        // Bodyguard - TODO
        // Larger Than Life - not useful in game
        // Long-Distance Taunt - TODO
        // Reactive Shield - already available
        // Shoulder Check - TODO
        
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
                new[] { Trait.Perception, Trait.Reflex, Trait.Athletics, Trait.Simple, Trait.Martial, Trait.Unarmed, Trait.Armor, Trait.UnarmoredDefense },
                new[] { Trait.Fortitude, Trait.Will },
                3,
                "{b}1. Guardian's Armor{/b} Even when you are struck, your armor protects you from some harm. You gain the armor specialization effects of medium and heavy armor.\n\n" +
                "{b}2. Intercept Strike {icon:Reaction}{/b} You keep your charges safe from harm, even if it means you get hurt yourself. You gain the Intercept Strike reaction.\n\n" +
                "{b}3. Shield Block {icon:Reaction}{/b} You gain the Shield Block general feat.\n\n" +
                "{b}4. Taunt{/b} Often, the best way to protect your allies is to have the enemy want to attack you instead. You gain the Taunt action.\n\n",
                null)
                .WithOnSheet((CalculatedCharacterSheetValues sheet) =>
                {
                    sheet.GrantFeat(FeatName.ShieldBlock);
                    sheet.GrantFeat(BattlecryMod.FeatName.InterceptStrike);
                    sheet.AddSelectionOption(new SingleFeatSelectionOption("GuardianFeat1", "Guardian feat", 1, (feat) => feat.HasTrait(BattlecryMod.Trait.Guardian)));
                    // FIXME: How to deal with dying2 once per day
                    // TODO: investigate what happens if they already have diehard
                    sheet.AddAtLevel(3, (sheet) => sheet.GrantFeat(FeatName.Diehard));
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
                                    if (!damageStuff.Kind.IsPhysical() || !protector.IsAdjacentTo(defender) || !protector.Actions.CanTakeReaction())
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
        }
    }
}
