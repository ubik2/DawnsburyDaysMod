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

namespace Dawnsbury.Mods.Remaster.FeatsDb.TrueFeatsDb
{

    public static class Cleric
    {
        // The following feats are excluded because they aren't useful enough in gameplay
        // * Premonition of Avoidance - no hazards in the game
        // * Sacred Ground - players heal entirely out of combat

        // The following should be addressed
        // * Communal Healing - this can choose a secondary heal target now
        // * Versatile Font - I think this should be doable

        // Not implemented because of difficulty, but I may reconsider
        // * Emblazon Armament - would need to choose weapon or shield
        // * Divine Infusion - I didn't do the infuse vitality spell either, but these are very similar
        // * Directed Channel - need to interact with the existing variant system, but this is a second 3 action variant
        // * Restorative Strike

        // * Divine Rebuttal - maybe?
        // * Divine Weapon - 
        // * Magic Hands - this would only affect Battle Medicine
        // * Selective Energy - TODO

        // * Restorative Channel - TODO
        // * Sanctify Armament
        // * Void Siphon
        // * Zealous Rush

        public static IEnumerable<Feat> LoadAll()
        {
            // Update the heal spell to enable our Panic the Dead feat.
            ClericClassFeatures.PatchHeal();

            // We treat a good aligmnent as being sanctified holy, and an evil alignment as being sanctified unholy.
            // This feat should also impact the harm spell, but we don't fight many holy creatures (there is a Lantern Archon in S1E5 which could be converted to a Cassisian).
            // I don't hide the Holy Castigation feat like I should.
            yield return new TrueFeat(RemasterFeats.FeatName.DivineCastigation, 1, "Your deity's grace doesn't extend to your sworn enemies.",
                "When you cast a {i}harm{/i} or {i}heal{/i} spell, you can add your holy or unholy trait to it. If you do, the spell deals damage to creatures with the opposing trait, even if it wouldn’t normally damage them. The spell deals spirit damage when used this way. For example, if you are holy, you could add the holy trait to a {i}heal{/i} spell and deal spirit damage to a fiend that has the unholy trait.",
                [Trait.Cleric])
                .WithPrerequisite((CalculatedCharacterSheetValues values) => values.NineCornerAlignment.GetTraits().Intersect([Trait.Good, Trait.Evil]).Any(), "You must have a Good or Evil alignment.")
                .WithPermanentQEffect("Your {i}heal{/i} spells damage fiends.", (QEffect qf) =>
                {
                    qf.Id = QEffectId.HolyCastigation;
                });

            // Panic the Dead (formerly Turn Undead)
            // I don't hide the Turn Undead feat like I should
            yield return new TrueFeat(RemasterFeats.FeatName.PanicTheDead, 2, "Vitality strikes terror in the undead.",
                "When you use a {i}heal{/i} spell to damage undead, any undead that fails its saving throw is also frightened 1. If it critically failed, the creature also gains the fleeing condition until the start of your next turn. Mindless undead are not immune to this effect due to being mindless.",
                [Trait.Cleric, Trait.Emotion, Trait.Fear, Trait.Mental])
                .WithPrerequisite((CalculatedCharacterSheetValues sheet) => sheet.AllFeats.Any(feat => feat.FeatName == FeatName.Warpriest), "You must be a warpriest.")
                .WithOnSheet((CalculatedCharacterSheetValues sheet) => sheet.SetProficiency(Trait.HeavyArmor, Proficiency.Trained));

            // Warpriest's Armor
            // The bulk reduction isn't implemented, since the game uses inventory slots instead of bulk.
            yield return new TrueFeat(RemasterFeats.FeatName.WarpriestsArmor, 2, "Your training has helped you adapt to ever - heavier armor.",
                "You are trained in heavy armor. Whenever you gain a class feature that grants you expert or greater proficiency in medium armor, you also gain that proficiency in heavy armor. You treat armor you wear of 2 Bulk or higher as though it were 1 Bulk lighter (to a minimum of 1 Bulk).",
                [Trait.Cleric])
                .WithPrerequisite((CalculatedCharacterSheetValues sheet) => sheet.AllFeats.Any(feat => feat.FeatName == FeatName.Warpriest), "You must be a warpriest.")
                .WithOnSheet((CalculatedCharacterSheetValues sheet) => sheet.SetProficiency(Trait.HeavyArmor, Proficiency.Trained));

            // Channel Smite
            yield return new TrueFeat(RemasterFeats.FeatName.ChannelSmite, 4, "You siphon the energies of life and death through a melee attack and into your foe.",
                "Make a melee Strike. On a hit, you cast the 1 - action version of the expended spell to damage the target, in addition to the normal damage from your Strike. The target automatically gets a failure on its save (or a critical failure if your Strike was a critical hit). The spell doesn’t have the manipulate trait when cast this way.\n\n" +
                "The spell is expended with no effect if your Strike fails or hits a creature that isn’t damaged by that energy type (such as if you hit a non - undead creature with a {i}heal{/i} spell).",
                [Trait.Cleric, Trait.Divine])
                .WithActionCost(2)
                .WithOnCreature((CalculatedCharacterSheetValues sheet, Creature creature) =>
                {
                    QEffect qfChannelSmite = new QEffect("Channel Smite {icon:TwoActions}", "You siphon the energies of life and death through a melee attack and into your foe.");
                    qfChannelSmite.ProvideStrikeModifierAsPossibility = (Item weapon) =>
                    {
                        if (!weapon.HasTrait(Trait.Melee))
                            return null;
                        Creature self = qfChannelSmite.Owner;
                        return ClericClassFeatures.CreateSmiteSpellcastingMenu(self, weapon, "Channel Smite");

                    };
                    creature.AddQEffect(qfChannelSmite);
                });

            yield return new TrueFeat(RemasterFeats.FeatName.RaiseSymbol, 4, "You present your religious symbol emphatically.",
                "You gain a +2 circumstance bonus to saving throws until the start of your next turn. While it's raised, if you roll a success at a saving throw against a vitality or void effect, you get a critical success instead.",
                [Trait.Cleric])
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
                        return new ActionPossibility(new CombatAction(qEffect.Owner, IllustrationName.GateAttenuator, "Raise Symbol", [Trait.Cleric],
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
                                    BonusToDefenses = (QEffect qEffect, CombatAction? action, Defense defense) =>
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
#if V3
                                    AdjustSavingThrowCheckResult = (QEffect qEffect, Defense defense, CombatAction action, CheckResult checkResult) =>
#else
                                    AdjustSavingThrowResult = (QEffect qEffect, CombatAction action, CheckResult checkResult) =>
#endif
                                    {
                                        // We use the old names here so we don't need to bring in symbols from the RemasterSpells mod.
                                        if (checkResult == CheckResult.Success && (action.HasTrait(Trait.Positive) || action.HasTrait(Trait.Negative)))
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

            // Deity selection
            foreach (Feat deitySelectionFeat in ClericClassFeatures.LoadDeitySelectionFeats())
            {
                yield return deitySelectionFeat;
            }

            // Update the class selection feat to reflect our updated deity list
            ClericClassFeatures.PatchClassDeities();
        }
    }
}
