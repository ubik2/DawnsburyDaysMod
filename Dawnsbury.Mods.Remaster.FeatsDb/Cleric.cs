using System.Collections.Generic;
using System.Linq;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using FeatName = Dawnsbury.Core.CharacterBuilder.Feats.FeatName;
using Trait = Dawnsbury.Core.Mechanics.Enumerations.Trait;

namespace Dawnsbury.Mods.Remaster.FeatsDb.TrueFeatsDb
{
    public static class Cleric
    {
        // The following feats are excluded because they aren't useful enough in gameplay
        // * Premonition of Avoidance
        // * Sacred Ground

        // Divine Castigation (formerly Holy Castigation)
        // Panic the Dead (formerly Turn Undead)
        // Warpriest's Armor
        // Channel Smite
        // Raise Symbol

        // Not implemented because of difficulty
        // * Emblazon Armament - would need to choose weapon or shield
        // * Divine Infusion - I didn't do infuse vitality either, but these are very similar
        // * Directed Channel - need to interact with the existing variant system, but this is a second 3 action variant
        // * Restorative Strike
        public static IEnumerable<Feat> LoadAll()
        {
            // Update the heal spell to enable our Panic the Dead feat.
            ClericClassFeatures.PatchHeal();

            // We treat a good aligmnent as being sanctified holy, and an evil alignment as being sanctified unholy.
            // This feat should also impact the harm spell, but we don't fight many holy creatures (there is a Lantern Archon in S1E5 which could be converted to a Cassisian).
            // I don't hide the Holy Castigation feat like I should.
            yield return new TrueFeat(RemasterFeats.FeatName.DivineCastigation, 1, "Your deity's grace doesn't extend to your sworn enemies.",
                "When you cast a {i}harm{/i} or {i}heal{/i} spell, you can add your holy or unholy trait to it. If you do, the spell deals damage to creatures with the opposing trait, even if it wouldn’t normally damage them. The spell deals spirit damage when used this way. For example, if you are holy, you could add the holy trait to a {i}heal{/i} spell and deal spirit damage to a fiend that has the unholy trait.",
                new[] { Trait.Cleric })
                .WithPrerequisite((CalculatedCharacterSheetValues values) => values.NineCornerAlignment.GetTraits().Intersect(new[] { Trait.Good, Trait.Evil }).Count() > 0, "You must have a Good or Evil alignment.")
                .WithPermanentQEffect("Your {i}heal{/i} spells damage fiends.", (QEffect qf) =>
                {
                    qf.Id = QEffectId.HolyCastigation;
                });

            // Panic the Dead (formerly Turn Undead)
            // I don't hide the Turn Undead feat like I should
            yield return new TrueFeat(RemasterFeats.FeatName.PanicTheDead, 2, "Vitality strikes terror in the undead.",
                "When you use a {i}heal{/i} spell to damage undead, any undead that fails its saving throw is also frightened 1. If it critically failed, the creature also gains the fleeing condition until the start of your next turn. Mindless undead are not immune to this effect due to being mindless.",
                new[] { Trait.Cleric, Trait.Emotion, Trait.Fear, Trait.Mental })
                .WithPrerequisite((CalculatedCharacterSheetValues sheet) => sheet.AllFeats.Any(feat => feat.FeatName == FeatName.Warpriest), "You must be a warpriest.")
                .WithOnSheet((CalculatedCharacterSheetValues sheet) => sheet.SetProficiency(Trait.HeavyArmor, Proficiency.Trained));

            // Warpriest's Armor
            // The bulk reduction isn't implemented, since the game uses inventory slots instead of bulk.
            yield return new TrueFeat(RemasterFeats.FeatName.WarpriestsArmor, 2, "Your training has helped you adapt to ever - heavier armor.",
                "You are trained in heavy armor. Whenever you gain a class feature that grants you expert or greater proficiency in medium armor, you also gain that proficiency in heavy armor. You treat armor you wear of 2 Bulk or higher as though it were 1 Bulk lighter (to a minimum of 1 Bulk).",
                new[] { Trait.Cleric })
                .WithPrerequisite((CalculatedCharacterSheetValues sheet) => sheet.AllFeats.Any(feat => feat.FeatName == FeatName.Warpriest), "You must be a warpriest.")
                .WithOnSheet((CalculatedCharacterSheetValues sheet) => sheet.SetProficiency(Trait.HeavyArmor, Proficiency.Trained));

            // Channel Smite
            yield return new TrueFeat(RemasterFeats.FeatName.ChannelSmite, 4, "You siphon the energies of life and death through a melee attack and into your foe.",
                "Make a melee Strike. On a hit, you cast the 1 - action version of the expended spell to damage the target, in addition to the normal damage from your Strike. The target automatically gets a failure on its save (or a critical failure if your Strike was a critical hit). The spell doesn’t have the manipulate trait when cast this way.\n\n" +
                "The spell is expended with no effect if your Strike fails or hits a creature that isn’t damaged by that energy type (such as if you hit a non - undead creature with a {i}heal{/i} spell).",
                new[] { Trait.Cleric, Trait.Divine })
                .WithActionCost(2)
                .WithOnCreature(((CalculatedCharacterSheetValues sheet, Creature creature) =>
                {
                    QEffect qfChannelSmite = new QEffect("Channel Smite {icon:TwoActions}", "You siphon the energies of life and death through a melee attack and into your foe.");
                    qfChannelSmite.ProvideStrikeModifierAsPossibility = (Item weapon) =>
                    {
                        if (!weapon.HasTrait(Trait.Melee))
                            return (Possibility)null;
                        Creature self = qfChannelSmite.Owner;
                        return (Possibility)ClericClassFeatures.CreateSmiteSpellcastingMenu(self, weapon, "Channel Smite");

                    };
                    creature.AddQEffect(qfChannelSmite);
                }));

            yield return new TrueFeat(RemasterFeats.FeatName.RaiseSymbol, 4, "You present your religious symbol emphatically.",
                "You gain a +2 circumstance bonus to saving throws until the start of your next turn. While it's raised, if you roll a success at a saving throw against a vitality or void effect, you get a critical success instead.",
                new[] { Trait.Cleric })
                .WithActionCost(1)
                .WithPermanentQEffect("You present your religious symbol emphatically.", (QEffect qEffect) =>
                {
                    qEffect.ProvideMainAction = (QEffect effect) =>
                    {
                        // If we already have the effect, don't provide the action possibility
                        if (qEffect.Owner.QEffects.Any((qEffect) => qEffect.Name == "Raise Symbol"))
                        {
                            return null;
                        }
                        return new ActionPossibility(new CombatAction(qEffect.Owner, IllustrationName.GateAttenuator, "Raise Symbol", new[] { Trait.Cleric },
                            "You present your religious symbol emphatically.",
                            // "\n\nIf the religious symbol you’re raising is a shield, such as with Emblazon Armaments, you gain the effects of Raise a Shield when you use this action and the effects of this action when you Raise a Shield." +
                            Target.Self().WithAdditionalRestriction((Creature caster) =>
                            {
                                return caster.HasFreeHand ? null : "You must be wielding a religious symbol or have a free hand.";
                            })).WithActionCost(1).WithEffectOnSelf((Creature caster) =>
                            {
                                QEffect raisedSymbolEffect = new QEffect("Raise Symbol", "Your symbol grants you a bonus to saving throws.")
                                {
                                    Illustration = IllustrationName.Shield,
                                    CountsAsABuff = true,
                                    BonusToDefenses = (QEffect qEffect, CombatAction action, Defense defense) =>
                                    {
                                        switch (defense) {
                                            case Defense.Fortitude:
                                            case Defense.Reflex:
                                            case Defense.Will:
                                                return new Bonus(2, BonusType.Circumstance, "raised symbol");
                                            default:
                                                return null;
                                        };
                                    },
                                    AdjustSavingThrowResult = (QEffect qEffect, CombatAction action, CheckResult checkResult) =>
                                    {
                                        if (checkResult == CheckResult.Success && (action.HasTrait(RemasterFeats.Trait.Vitality) || action.HasTrait(RemasterFeats.Trait.Void)))
                                        {
                                            return CheckResult.CriticalSuccess;
                                        }
                                        return checkResult;
                                    }
                                }.WithExpirationAtStartOfOwnerTurn();
                                caster.AddQEffect(raisedSymbolEffect);
                            }));
                    };
                });

            // Cleric Fonts
            foreach (Feat fontFeat in ClericClassFeatures.LoadFonts())
            {
                yield return fontFeat;
            }
        }

    }

    //CombatAction? CreateSpellstrike(CombatAction spell)
    //{
    //    if (spell.Variants != null)
    //        return (CombatAction)null;
    //    if (spell.SubspellVariants != null)
    //        return (CombatAction)null;
    //    if (spell.ActionCost != 1 && spell.ActionCost != 2)
    //        return (CombatAction)null;
    //    if (!spell.HasTrait(Trait.Attack))
    //        return (CombatAction)null;
    //    CombatAction strike = qfSpellstrike.Owner.CreateStrike(weapon);
    //    strike.Name = spell.Name;
    //    strike.Illustration = (Illustration)new SideBySideIllustration(strike.Illustration, spell.Illustration);
    //    strike.Traits.AddRange(spell.Traits.Except<Trait>((IEnumerable<Trait>)new Trait[4]
    //    {
    //        Trait.Ranged,
    //        Trait.Prepared,
    //        Trait.Spontaneous,
    //        Trait.Spell
    //    }));
    //    strike.Traits.Add(Trait.Spellstrike);
    //    strike.Traits.Add(Trait.Basic);
    //    strike.ActionCost = 2;
    //    ((CreatureTarget)strike.Target).WithAdditionalConditionOnTargetCreature((Func<Creature, Creature, Usability>)((a, d) => a.HasEffect(QEffectId.SpellstrikeDischarged) ? Usability.NotUsable("You must first recharge your Spellstrike by spending an action or casting a focus spell.") : Usability.Usable));
    //    strike.StrikeModifiers.OnEachTarget = (Func<Creature, Creature, CheckResult, Task>)(async (a, d, result) =>
    //    {
    //        Steam.CollectAchievement("MAGUS");
    //        a.Spellcasting.UseUpSpellcastingResources(spell);
    //        if (result >= CheckResult.Success)
    //        {
    //            if (spell.EffectOnOneTarget != null)
    //                await spell.EffectOnOneTarget(spell, a, d, result);
    //            if (spell.EffectOnChosenTargets != null)
    //                await spell.EffectOnChosenTargets(spell, a, new ChosenTargets()
    //                {
    //                    ChosenCreature = d,
    //                    ChosenCreatures = {
    //                        d
    //                    }
    //                });
    //        }
    //        a.AddQEffect(new QEffect()
    //        {
    //            Id = QEffectId.SpellstrikeDischarged,
    //            AfterYouTakeAction = (Func<QEffect, CombatAction, Task>)((qfDischarge, action) =>
    //            {
    //                if (!action.HasTrait(Trait.Focus))
    //                    return;
    //                qfDischarge.ExpiresAt = ExpirationCondition.Immediately;
    //            }),
    //            ProvideMainAction = (Func<QEffect, Possibility>)(qfDischarge => (Possibility)(ActionPossibility)new CombatAction(qfDischarge.Owner, (Illustration)IllustrationName.Good, "Recharge Spellstrike", new Trait[2]
    //            {
    //                Trait.Concentrate,
    //                Trait.Basic
    //            }, "Recharge your Spellstrike so that you can use it again." + (qfDischarge.Owner.HasEffect(QEffectId.MagussConcentration) ? " {Blue}You gain a +1 circumstance bonus to your next attack until the end of your next turn.{/Blue}" : ""), (Target)Target.Self()).WithActionCost(1).WithSoundEffect(SfxName.AuraExpansion).WithEffectOnSelf((Func<Creature, Task>)(self2 =>
    //            {
    //                qfDischarge.ExpiresAt = ExpirationCondition.Immediately;
    //                if (!self2.HasEffect(QEffectId.MagussConcentration))
    //                    return;
    //                self2.AddQEffect(new QEffect("Magus's Concentration", "You have +1 to your next attack roll.", ExpirationCondition.ExpiresAtEndOfSourcesTurn, self2, (Illustration)IllustrationName.Good)
    //                {
    //                    CannotExpireThisTurn = true,
    //                    BonusToAttackRolls = (Func<QEffect, CombatAction, Creature, Bonus>)((qf, ca, df) => !ca.HasTrait(Trait.Attack) ? (Bonus)null : new Bonus(1, BonusType.Circumstance, "Magus's Concentration")),
    //                    AfterYouTakeAction = (Func<QEffect, CombatAction, Task>)((qf, ca) =>
    //                    {
    //                        if (!ca.HasTrait(Trait.Attack))
    //                            return;
    //                        qf.ExpiresAt = ExpirationCondition.Immediately;
    //                    })
    //                });
    //            })))
    //        });
    //    });
    //    strike.Description = StrikeRules.CreateBasicStrikeDescription(strike.StrikeModifiers, additionalSuccessText: "The success effect of " + spell.Name + " is inflicted upon the target.", additionalCriticalSuccessText: "Critical spell effect.", additionalAftertext: "You can't use Spellstrike again until you recharge it by spending an action or casting a focus spell.");
    //    return strike;
    //}

}
