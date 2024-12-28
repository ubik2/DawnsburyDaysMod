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
using Dawnsbury.Modding;

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

            // Update to grant expert proficiency with martial weapons as part of the warpriest's third doctrune at level 7
            ClericClassFeatures.PatchDoctrine();

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

            // Emblazon Armament
            yield return new TrueFeat(RemasterFeats.FeatName.EmblazonArmament, 2, "Carefully etching a sacred image into a physical object, you steel yourself for battle.",
                "You can spend 10 minutes emblazoning a symbol of your deity upon a weapon or shield. The symbol doesn't fade until 1 year has passed, but if you Emblazon an Armament, any symbol you previously emblazoned, and any symbol already emblazoned on that item instantly disappears. The emblazoned item is a religious symbol of your deity in addition to its normal purpose, and it gains another benefit determined by the type of item. The benefit applies only to followers of the deity the symbol represents, but others can use the item normally.",
                [Trait.Cleric]) // Also Exploration, but that's not implemented
                .WithPermanentQEffect("Your holy symbol is emblazoned on your shield.", (QEffect qf) =>
                {
                    qf.Description = "The shield gains a +1 status bonus to its Hardness.";
                    qf.StartOfCombat = async (QEffect qf2) =>
                    {
                        Item? shield = qf2.Owner.HeldItems.FirstOrDefault((item) => item.HasTrait(Trait.Shield));
                        if (shield != null)
                        {
                            shield.Hardness += 1;
                        }
                    };
                });

            // Panic the Dead (formerly Turn Undead)
            // I don't hide the Turn Undead feat like I should.
            // This is implemented similarly to the Turn Undead feat, where it's the Heal spell that's actually modified to check for this feat.
            yield return new TrueFeat(RemasterFeats.FeatName.PanicTheDead, 2, "Vitality strikes terror in the undead.",
                "When you use a {i}heal{/i} spell to damage undead, any undead that fails its saving throw is also frightened 1. If it critically failed, the creature also gains the fleeing condition until the start of your next turn. Mindless undead are not immune to this effect due to being mindless.",
                [Trait.Cleric, Trait.Emotion, Trait.Fear, Trait.Mental]);

            // Warpriest's Armor
            // The bulk reduction isn't implemented, since the game uses inventory slots instead of bulk.
            // We don't advance the proficiency, since that doesn't apply until level 13, and the game doesn't go that high
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
                        Creature owner = qEffect.Owner;
                        // If we already have the effect, don't provide the action possibility
                        if (owner.QEffects.Any((qEffect) => qEffect.Name == "Symbol raised"))
                        {
                            return null;
                        }
                        // If we already have an emblazoned shield, we don't need a special action since it will happen with our Raise shield
                        if (GetEmblazonedShield(owner) != null)
                        {
                            return null;
                        }
                        return new ActionPossibility(new CombatAction(owner, IllustrationName.GateAttenuator, "Raise symbol", [Trait.Cleric],
                            "You present your religious symbol emphatically." +
                            "\nIf the religious symbol you’re raising is a shield, such as with Emblazon Armaments, you gain the effects of Raise a Shield when you use this action and the effects of this action when you Raise a Shield.",
                            Target.Self().WithAdditionalRestriction((Creature caster) =>
                            {
                                if (caster.HasFreeHand)
                                {
                                    return null;
                                }
                                return "You must be wielding a religious symbol or have a free hand.";
                            })).WithActionCost(1).WithEffectOnSelf((Creature caster) =>
                            {
                                caster.AddQEffect(CreateRaiseSymbolEffect(caster));
                            }));

                    };
                    qEffect.AfterYouTakeAction = async (QEffect self, CombatAction action) =>
                    {
                        // There's a couple potential Raise Shield variants, and we need to have an emblazoned shield
                        if (action.Name.StartsWith("Raise shield", StringComparison.OrdinalIgnoreCase) && 
                            GetEmblazonedShield(action.Owner) != null)
                        {
                            action.Owner.AddQEffect(CreateRaiseSymbolEffect(action.Owner));
                        }
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

        private static Item? GetEmblazonedShield(Creature caster)
        {
            Item? emblazonedShield = null;
            // Check to see if they have the Cleric Warmastered trait on their shield (from that mod's version of the feat)
            if (ModManager.TryParse("Emblazoned", out Trait emblazonedTrait))
            {
                emblazonedShield = caster.HeldItems.FirstOrDefault((item) => item.HasTrait(Trait.Shield) && item.HasTrait(emblazonedTrait));
            }
            // If we don't have that, try to see if they're using our variant
            if (emblazonedShield == null)
            {
                // I'm more permissive, and let them use any shield if they have the feat.
                // This can misbehave if they have shield1 without the emblazon, and shield2 with emblazon, since they can block with shield1 and
                // won't benefit from the hardness on shield2.
                if (caster.HasFeat(RemasterFeats.FeatName.EmblazonArmament))
                {
                    emblazonedShield = caster.HeldItems.FirstOrDefault((item) => item.HasTrait(Trait.Shield));
                }
            }
            return emblazonedShield;
        }

        private static QEffect CreateRaiseSymbolEffect(Creature caster)
        {
            return new QEffect("Symbol raised", "Your symbol grants you a bonus to saving throws.")
            {
                Illustration = IllustrationName.GateAttenuator,
                CountsAsABuff = true,
                BonusToDefenses = (QEffect qEffect, CombatAction? action, Defense defense) =>
                {
                    switch (defense)
                    {
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
        }
    }
}
