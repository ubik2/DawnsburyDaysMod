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
            yield return PatchBloodline(FeatName.ImperialBloodline, Trait.Arcane, SpellId.UnravelingBlast, SpellId.Thunderburst, [SpellId.AncientDust, SpellId.MagicMissile, SpellId.Invisibility, SpellId.Haste, SpellId.DimensionDoor],
                "You and the target, if friendly, each get a +1 status bonus to all skill checks for 1 round.");
            yield return PatchBloodline(FeatName.AngelicBloodline, Trait.Divine, SpellId.AngelicHalo, SpellId.AngelicWings, [SpellId.DivineLance, SpellId.Heal, SpellId.SpiritualWeapon, SpellId.SearingLight, SpellId.DivineWrath],
                "You and all friendly targets each get a +1 status bonus to all saving throws for 1 round.");
            yield return PatchBloodline(FeatName.DemonicBloodline, Trait.Divine, SpellId.GluttonsJaw, SpellId.SwampOfSloth, [SpellId.AcidSplash, SpellId.Fear, SpellId.HideousLaughter, SpellId.Slow, SpellId.DivineWrath],
                "All enemy targets get a -1 status penalty to AC for 1 round, and you get a +1 status bonus to Intimidation for 1 round.");
            yield return PatchBloodline(FeatName.InfernalBloodline, Trait.Divine, SpellId.RejuvenatingFlames, SpellId.ShieldOfDivineFire, [SpellId.ProduceFlame, SpellId.BurningHands, SpellId.FlamingSphere, SpellId.CrisisOfFaith, SpellId.DivineWrath],
                "All enemy targets take 1 extra fire damage per spell level.");
            yield return PatchBloodline(FeatName.DraconicBloodline, Trait.Arcane, SpellId.DragonClaws, SpellId.DragonBreath, [SpellId.Shield, SpellId.TrueStrike, SpellId.ResistEnergy, SpellId.Haste, SpellId.SpellImmunity],
                "You and the target each get a +1 status bonus to AC for 1 round."); // FIXME: Need to address the OnSheet
            yield return PatchBloodline(FeatName.HagBloodline, Trait.Occult, SpellId.JealousHex, SpellId.HorrificVisage, [SpellId.Daze, SpellId.Fear, SpellId.TouchOfIdiocy, SpellId.Blindness, SpellId.BestowCurse],
                "The first creature that deals damage to you before the end of your next turn takes 2 mental damage per spell level (basic Will save mitigates).");
            yield return PatchBloodline(FeatName.FireElementalBloodline, Trait.Primal, SpellId.ElementalToss, SpellId.FieryWings, [SpellId.ProduceFlame, SpellId.BurningHands, SpellId.ResistEnergy, SpellId.Fireball, SpellId.FireShield],
                "All enemy targets take 1 extra fire damage per spell level, and you get a +1 status bonus to Intimidation for 1 round.");

            PatchClassBloodlines();
        }

        /// <summary>
        ///  After adding our bloodline selection feats, we need to update the ClassSelectionFeat feat (since it has its own copy of this list)
        /// </summary>
        public static void PatchClassBloodlines()
        {
            FeatName[] bloodlineFeatNames = [FeatName.ImperialBloodline, FeatName.AngelicBloodline, FeatName.DemonicBloodline, FeatName.InfernalBloodline, FeatName.DraconicBloodline, FeatName.HagBloodline, FeatName.FireElementalBloodline];
            ClassSelectionFeat classFeat = (ClassSelectionFeat)AllFeats.All.First((feat) => feat.FeatName == FeatName.Sorcerer);
            classFeat.Subfeats = AllFeats.All.Where((feat) => bloodlineFeatNames.Contains(feat.FeatName)).ToList();
        }

        // I'm able to pull the FeatName and FlavorText from the original feat.
        // I can't directly pull the WithBloodMagic, but I can replicate the changes to both the RulesText and capture the existing feat's functionality
        // through the OnCreature function.
        // In the case of the DraconicBloodline, I also need to add the OnSheet hook that sets up the dragon type. I can't just copy this, because we've changed other bits.
        private static Bloodline PatchBloodline(FeatName bloodlineFeatName, Trait spellList, SpellId focusSpellId, SpellId advancedFocusSpellId, SpellId[] grantedSpells, string bloodMagicDescription)
        {
            Bloodline existingFeat = (Bloodline)AllFeats.All.First((feat) => feat.FeatName == bloodlineFeatName);
            focusSpellId = RemasterFeats.GetUpdatedSpellId(focusSpellId);
            advancedFocusSpellId = RemasterFeats.GetUpdatedSpellId(advancedFocusSpellId);
            grantedSpells = grantedSpells.Select(RemasterFeats.GetUpdatedSpellId).ToArray();
            string basicRulesText = "• Spell list: {b}" + spellList.ToString() + "{/b} {i}" + ExplainSpellList(spellList) + "{/i}\n" +
                "• Focus spell: " + AllSpells.CreateModernSpellTemplate(focusSpellId, Trait.Sorcerer).ToSpellLink() + "\n" +
                "• Bloodline-granted spells: cantrip: " + AllSpells.CreateModernSpellTemplate(grantedSpells[0], Trait.Sorcerer).ToSpellLink() + 
                (grantedSpells.Length >= 1 ? ", 1st: " + AllSpells.CreateModernSpellTemplate(grantedSpells[1], Trait.Sorcerer).ToSpellLink() : "") +
                (grantedSpells.Length >= 2 ? ", 2nd: " + AllSpells.CreateModernSpellTemplate(grantedSpells[2], Trait.Sorcerer).ToSpellLink() : "") +
                (grantedSpells.Length >= 3 ? ", 3rd: " + AllSpells.CreateModernSpellTemplate(grantedSpells[3], Trait.Sorcerer).ToSpellLink() : "") +
                (grantedSpells.Length >= 4 ? ", 4th: " + AllSpells.CreateModernSpellTemplate(grantedSpells[4], Trait.Sorcerer).ToSpellLink() : "") +
                "\n" +
                "• Blood magic effect: " + bloodMagicDescription + " {i}(Your blood magic effect activates whenever you use your focus spell or a non-cantrip bloodline-granted spell.){/i}";
            Bloodline newFeat = new Bloodline(existingFeat.FeatName, existingFeat.FlavorText ?? "", spellList, focusSpellId, advancedFocusSpellId, grantedSpells )
            {
                RulesText = basicRulesText,
                OnCreature = existingFeat.OnCreature
            };
            if (newFeat.FeatName == FeatName.DraconicBloodline)
            {
                newFeat.OnSheet += (sheet) => sheet.AddSelectionOption(new SingleFeatSelectionOption("DraconicAncestorType", "Ancestor dragon type", 1, (feat) => feat.HasTrait(Trait.AncestorDragonTypeFeat)));
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
