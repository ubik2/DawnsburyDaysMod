using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.Mechanics.Enumerations;

namespace Dawnsbury.Mods.Remaster.FeatsDb
{
    public static class Sorcerer
    {
        public static IEnumerable<Feat> LoadAll()
        {
            yield return PatchBloodline(FeatName.ImperialBloodline, Trait.Arcane, SpellId.UnravelingBlast, SpellId.AncientDust, SpellId.MagicMissile, SpellId.Invisibility,
                "You and the target, if friendly, each get a +1 status bonus to all skill checks for 1 round.");
            yield return PatchBloodline(FeatName.AngelicBloodline, Trait.Divine, SpellId.AngelicHalo, SpellId.DivineLance, SpellId.Heal, SpellId.SpiritualWeapon,
                "You and all friendly targets each get a +1 status bonus to all saving throws for 1 round.");
            yield return PatchBloodline(FeatName.DemonicBloodline, Trait.Divine, SpellId.GluttonsJaw, SpellId.AcidSplash, SpellId.Fear, SpellId.HideousLaughter,
                "All enemy targets get a -1 status penalty to AC for 1 round, and you get a +1 status bonus to Intimidation for 1 round.");
            yield return PatchBloodline(FeatName.InfernalBloodline, Trait.Divine, SpellId.RejuvenatingFlames, SpellId.ProduceFlame, SpellId.BurningHands, SpellId.FlamingSphere,
                "All enemy targets take 1 extra fire damage per spell level.");
            yield return PatchBloodline(FeatName.DraconicBloodline, Trait.Arcane, SpellId.DragonClaws, SpellId.Shield, SpellId.TrueStrike, SpellId.ResistEnergy,
                "You and the target each get a +1 status bonus to AC for 1 round."); // FIXME: Need to address the OnSheet
            yield return PatchBloodline(FeatName.HagBloodline, Trait.Occult, SpellId.JealousHex, SpellId.Daze, SpellId.Fear, SpellId.TouchOfIdiocy,
                "The first creature that deals damage to you before the end of your next turn takes 2 mental damage per spell level (basic Will save mitigates).");
            yield return PatchBloodline(FeatName.FireElementalBloodline, Trait.Primal, SpellId.ElementalToss, SpellId.ProduceFlame, SpellId.BurningHands, SpellId.ResistEnergy,
                "All enemy targets take 1 extra fire damage per spell level, and you get a +1 status bonus to Intimidation for 1 round.");

            PatchClassBloodlines();
        }

        /// <summary>
        ///  After adding our bloodline selection feats, we need to update the ClassSelectionFeat feat (since it has its own copy of this list)
        /// </summary>
        public static void PatchClassBloodlines()
        {
            FeatName[] bloodlineFeatNames = new[] { FeatName.ImperialBloodline, FeatName.AngelicBloodline, FeatName.DemonicBloodline, FeatName.InfernalBloodline, FeatName.DraconicBloodline, FeatName.HagBloodline, FeatName.FireElementalBloodline };
            ClassSelectionFeat classFeat = (ClassSelectionFeat)AllFeats.All.First((feat) => feat.FeatName == FeatName.Sorcerer);
            classFeat.Subfeats = AllFeats.All.Where((feat) => bloodlineFeatNames.Contains(feat.FeatName)).ToList();
        }

        // I'm able to pull the FeatName and FlavorText from the original feat.
        // I can't directly pull the WithBloodMagic, but I can replicate the changes to both the RulesText and capture the existing feat's functionality
        // through the OnCreature function.
        // In the case of the DraconicBloodline, I also need to add the OnSheet hook that sets up the dragon type. I can't just copy this, because we've changed other bits.
        private static Bloodline PatchBloodline(FeatName bloodlineFeatName, Trait spellList, SpellId focusSpellId, SpellId cantripSpellId, SpellId level1SpellId, SpellId level2SpellId, string bloodMagicDescription)
        {
            Bloodline existingFeat = (Bloodline)AllFeats.All.First((feat) => feat.FeatName == bloodlineFeatName);
            focusSpellId = RemasterFeats.GetUpdatedSpellId(focusSpellId);
            cantripSpellId = RemasterFeats.GetUpdatedSpellId(cantripSpellId);
            level1SpellId = RemasterFeats.GetUpdatedSpellId(level1SpellId);
            level2SpellId = RemasterFeats.GetUpdatedSpellId(level2SpellId);
            string basicRulesText = "• Spell list: {b}" + spellList.ToString() + "{/b} {i}" + ExplainSpellList(spellList) + "{/i}\n" +
                "• Focus spell: " + AllSpells.CreateModernSpellTemplate(focusSpellId, Trait.Sorcerer).ToSpellLink() + "\n" +
                "• Bloodline-granted spells: cantrip: " + AllSpells.CreateModernSpellTemplate(cantripSpellId, Trait.Sorcerer).ToSpellLink() + ", 1st: " + AllSpells.CreateModernSpellTemplate(level1SpellId, Trait.Sorcerer).ToSpellLink() + ", 2nd: " + AllSpells.CreateModernSpellTemplate(level2SpellId, Trait.Sorcerer).ToSpellLink() + "\n" +
                "• Blood magic effect: " + bloodMagicDescription + " {i}(Your blood magic effect activates whenever you use your focus spell or a non-cantrip bloodline-granted spell.){/i}";
            Bloodline newFeat = new Bloodline(existingFeat.FeatName, existingFeat.FlavorText ?? "", spellList, focusSpellId, cantripSpellId, level1SpellId, level2SpellId)
            {
                RulesText = basicRulesText,
                OnCreature = existingFeat.OnCreature
            };
            if (newFeat.FeatName == FeatName.DraconicBloodline)
            {
                newFeat.OnSheet += ((sheet) => sheet.AddSelectionOption(new SingleFeatSelectionOption("DraconicAncestorType", "Ancestor dragon type", 1, ((feat) => feat.HasTrait(Trait.AncestorDragonTypeFeat)))));
            }
            return newFeat;
        }

        // Just a copy of the one in Bloodline
        private static string ExplainSpellList(Trait spellList)
        {
            switch (spellList)
            {
                case Trait.Arcane:
                    return "(You cast arcane spells. Arcane spells are extremely varied, include powerful offensive and debuffing spells, but cannot heal your allies. Arcane sorcerers, wizards and magi can cast arcane spells.)";
                case Trait.Primal:
                    return "(You cast primal spells. Primal spells are very varied, but focus on elemental and energy effects, including both dealing damage and healing, but generally can't affect minds. Primal sorcerers and druids can cast primal spells.)";
                case Trait.Occult:
                    return "(You can cast occult spells. Occult spells focus on enchantment, emotion and the mind. They inflict debuffs on your opponents and grant buffs to your allies, but generally can't manipulate energy. Occult sorcerers and psychics can cast occult spells.)";
                default:
                    return "(You cast divine spells. Divine spells can heal or buff your allies and are powerful against the undead, but they can lack utility or offensive power against natural creatures. Divine sorcerers and clerics can cast divine spells.)";
            }
        }
    }
}
