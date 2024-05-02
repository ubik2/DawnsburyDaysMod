using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Modding;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.Roller;

namespace Dawnsbury.Mods.Remaster.FeatsDb
{
    public static class ClericClassFeatures
    {
        public static IEnumerable<Feat> LoadFonts()
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
        }


        // Patch the heal spell to handle the Panic the Dead feat
        public static void PatchHeal()
        {
            ModManager.RegisterActionOnEachSpell((CombatAction originalSpell) => {
                if (originalSpell.SpellId != SpellId.Heal || originalSpell.Owner == null || !originalSpell.Owner.HasFeat(RemasterFeats.FeatName.PanicTheDead))
                {
                    return;
                }
                Delegates.EffectOnEachTarget extraEffect = async delegate (CombatAction spell, Creature caster, Creature target, CheckResult checkResult)
                {
                    // This overlaps some with the base implementation, but the remaster version doesn't restrict by level and adds the frighteneed 1 on failure.
                    if (target.HasTrait(Trait.Undead))
                    {
                        if (checkResult != CheckResult.CriticalSuccess)
                        {
                            target.AddQEffect(QEffect.Frightened(1));
                        }
                        if (checkResult == CheckResult.CriticalFailure)
                        {
                            target.AddQEffect(QEffect.Fleeing(caster).WithExpirationAtStartOfSourcesTurn(caster, 1));
                        }
                    }
                    await Task.Delay(0);
                };
                originalSpell.EffectOnOneTarget = (Delegates.EffectOnEachTarget)Delegate.Combine(originalSpell.EffectOnOneTarget, extraEffect);
            });
        }

        public static SubmenuPossibility CreateSmiteSpellcastingMenu(Creature caster, Item weapon, string caption)
        {
            List<CombatAction> smiteSpells = new List<CombatAction>();
            foreach (SpellcastingSource source in caster.Spellcasting.Sources)
            {
                smiteSpells.AddRange(source.Spells.Where((spell) => spell.SpellId == SpellId.Harm || spell.SpellId == SpellId.Heal));
            }
            IEnumerable<Possibility> possibilityList = smiteSpells.Select(delegate (CombatAction spell) {
                CombatAction strikeAction = caster.CreateStrike(weapon).WithActionCost(2);
                strikeAction.Name = caption + ": " + spell.Name + " " + spell.SpellLevel;
                StrikeModifiers strikeModifiers = strikeAction.StrikeModifiers;
                strikeModifiers.OnEachTarget = (Func<Creature, Creature, CheckResult, Task>)Delegate.Combine(strikeModifiers.OnEachTarget, async delegate (Creature caster, Creature target, CheckResult checkResult)
                {                 
                    caster.Spellcasting?.UseUpSpellcastingResources(spell);
                    if (target.DeathScheduledForNextStateCheck)
                    {
                        return;
                    }
                    bool canDamage = (spell.SpellId == SpellId.Heal && (target.HasTrait(Trait.Undead) || (caster.HasFeat(RemasterFeats.FeatName.DivineCastigation) && target.HasTrait(Trait.Fiend)))) ||
                        (spell.SpellId == SpellId.Harm && !target.HasTrait(Trait.Undead));
                    if (canDamage)
                    {
                        CheckResult savingThrowResult = checkResult switch
                        {
                            CheckResult.CriticalSuccess => CheckResult.CriticalFailure,
                            CheckResult.Success => CheckResult.Failure,
                            _ => CheckResult.CriticalSuccess
                        };
                        int dieValue = ((spell.SpellId == SpellId.Heal && caster.HasFeat(FeatName.HealingFont)) || (spell.SpellId == SpellId.Harm && caster.HasFeat(FeatName.HarmingHands))) ? 10 : 8;
                        bool isHeal = spell.SpellId == SpellId.Heal;
                        await CommonSpellEffects.DealBasicDamage(spell, caster, target, savingThrowResult, DiceFormula.FromText(spell.SpellLevel.ToString() + "d" + dieValue.ToString(), isHeal ? "Heal vitality damage" : "Harm void damage"), isHeal ? DamageKind.Positive : DamageKind.Negative);
                    }
                    else
                    {
                        caster.Battle.Log(spell.Name + " is ineffective.");
                    }
                });
                return new ActionPossibility(strikeAction);
            });
            SubmenuPossibility channelSmite = new SubmenuPossibility((Illustration)IllustrationName.TrueStrike, caption);
            PossibilitySection possibilitySection = new PossibilitySection(caption);
            possibilitySection.Possibilities.AddRange(possibilityList);
            channelSmite.Subsections.Add(possibilitySection);
            return channelSmite;
        }
    }
}
