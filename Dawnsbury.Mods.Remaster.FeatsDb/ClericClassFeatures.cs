using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core;
using System;
using System.Collections.Generic;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
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
    }
}
