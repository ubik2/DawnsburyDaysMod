using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.Mechanics.Enumerations;
using System;
using System.Collections.Generic;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Modding;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Mechanics;
using System.Linq;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Display.Illustrations;
using System.Threading.Tasks;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Mechanics.Targeting;

namespace Dawnsbury.Mods.Spellbook.RemasterSpells
{
    public static class Feats
    {
        public static IEnumerable<Feat> GetFeats()
        {

            yield return new Feat(FeatName.HealingFont, "Through your deity's blessing, you gain additional spells that channel the life force called vitality.", "You gain 4 additional spell slots each day at your highest level of cleric spell slots. You automatically prepare {i}heal{/i} spells in these slots.", new List<Trait> { Trait.DivineFont }, null).WithOnSheet(delegate (CalculatedCharacterSheetValues sheet)
            {
                sheet.AtEndOfRecalculation = (Action<CalculatedCharacterSheetValues>)Delegate.Combine(sheet.AtEndOfRecalculation, delegate (CalculatedCharacterSheetValues values)
                {
                    int num = 4;
                    for (int j = 0; j < num; j++)
                    {
                        values.PreparedSpells[Trait.Cleric].Slots.Add(new EnforcedPreparedSpellSlot(values.MaximumSpellLevel, "Healing Font", AllSpells.CreateModernSpellTemplate(SpellId.Heal, Trait.Cleric), "HealingFont:" + j));
                    }
                });
            }).WithIllustration(IllustrationName.Heal);
            yield return new Feat(FeatName.HarmfulFont, "Through your deity's blessing, you gain additional spells that channel the counterforce to life, the void.", "You gain 4 additional spell slots each day at your highest level of cleric spell slots. You automatically prepare {i}harm{/i} spells in these slots.", new List<Trait> { Trait.DivineFont }, null).WithOnSheet(delegate (CalculatedCharacterSheetValues sheet)
            {
                sheet.AtEndOfRecalculation = (Action<CalculatedCharacterSheetValues>)Delegate.Combine(sheet.AtEndOfRecalculation, delegate (CalculatedCharacterSheetValues values)
                {
                    int num = 4;
                    for (int i = 0; i < num; i++)
                    {
                        values.PreparedSpells[Trait.Cleric].Slots.Add(new EnforcedPreparedSpellSlot(values.MaximumSpellLevel, "Harmful Font", AllSpells.CreateModernSpellTemplate(SpellId.Harm, Trait.Cleric), "HarmfulFont:" + i));
                    }
                });
            }).WithIllustration(IllustrationName.Harm);

            yield return SlamDown();
        }

        public static void ReplaceExistingFeats()
        {
            var newFeats = Feats.GetFeats();
            // Remove any feats that have the same name as one of our new feats
            AllFeats.All.RemoveAll((feat) => newFeats.Any((newFeat) => newFeat.FeatName == feat.FeatName));
            foreach (var feat in newFeats)
            {
                ModManager.AddFeat(feat);
            }
        }

        public static Feat SlamDown()
        {
            Feat feat = new TrueFeat(FeatName.CustomFeat, 4, "You make an attack to knock a foe off balance, then follow up immediately with a sweep to topple them.",
                "Make a melee Strike. If it hits and deals damage, you can attempt an Athletics check to Trip the creature you hit. If you’re wielding a two-handed melee weapon, you can ignore Trip's requirement that you have a hand free. Both attacks count toward your multiple attack penalty, but the penalty doesn't increase until after you've made both of them.",
                new[] { Trait.Fighter, Trait.Flourish })
            .WithActionCost(2)
            .WithCustomName("Slam Down")
            .WithPermanentQEffect("You make an attack to knock a foe off balance, then follow up immediately with a sweep to topple them.", delegate (QEffect caster)
            {
                caster.ProvideStrikeModifier = delegate (Item item)
                {
                    CombatAction combatAction = caster.Owner.CreateStrike(item).WithActionCost(2);
                    combatAction.Traits.Add(Trait.Flourish);
                    combatAction.Illustration = new SideBySideIllustration(combatAction.Illustration, IllustrationName.Trip);
                    combatAction.Name = "Slam Down";
                    combatAction.Description = StrikeRules.CreateBasicStrikeDescription(combatAction.StrikeModifiers, null, "You can attempt an Athletics check to Trip the creature you hit.", "You can attempt an Athletics check to Trip the creature you hit.");
                    StrikeModifiers strikeModifiers = combatAction.StrikeModifiers;
                    strikeModifiers.OnEachTarget = (Func<Creature, Creature, CheckResult, Task>)Delegate.Combine(strikeModifiers.OnEachTarget, (Func<Creature, Creature, CheckResult, Task>)async delegate (Creature caster, Creature target, CheckResult checkResult)
                    {
                        // TODO: also need to check to see if we do damage
                        if (checkResult >= CheckResult.Success)
                        {
                            CombatAction tripAction = Possibilities.CreateTrip(caster);
                            tripAction.ChosenTargets = new ChosenTargets
                            {
                                ChosenCreature = target
                            };
                            await tripAction.WithActionCost(0).AllExecute();
                        }
                    });
                    ((CreatureTarget)combatAction.Target).WithAdditionalConditionOnTargetCreature((Creature self, Creature target) => (!self.HasFreeHand && !item.HasTrait(Trait.TwoHanded)) ? Usability.CommonReasons.NoFreeHandForManeuver : Usability.Usable);
                    return combatAction;
                };
            });
            feat.Prerequisites.Add(new Prerequisite((CalculatedCharacterSheetValues sheet) => sheet.GetProficiency(Trait.Athletics) >= Proficiency.Trained, "You must be trained in Athletics.")); 
            return feat;
        }
    }
}

