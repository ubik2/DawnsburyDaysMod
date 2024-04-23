using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Modding;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dawnsbury.Mods.Remaster.HideLegacySpells
{
    public class HideLegacySpells
    {
        [DawnsburyDaysModMainMethod]
        public static void LoadMod()
        {
            // A list of legacy spell ids for each level (starting at 0)
            SpellId[][] legacySpells = new[] {
                new[] { SpellId.AcidSplash, SpellId.RayOfFrost, SpellId.ProduceFlame, SpellId.DisruptUndead, SpellId.ChillTouch },
                new[] { SpellId.BurningHands, SpellId.ColorSpray, SpellId.MagicMissile, SpellId.MageArmor, SpellId.MagicWeapon, SpellId.TrueStrike, SpellId.ShockingGrasp },
                new[] { SpellId.AcidArrow, SpellId.CalmEmotions, SpellId.FlamingSphere, SpellId.HideousLaughter, SpellId.ObscuringMist, SpellId.SoundBurst, SpellId.Barkskin, SpellId.SpiritualWeapon, SpellId.TouchOfIdiocy }
            };
            for (int i = 0; i < legacySpells.Length; i++)
            {
                foreach (var legacySpell in legacySpells[i])
                {
                    HideLegacySpell(legacySpell, i);
                }
            }
        }

        public static void HideLegacySpell(SpellId legacySpellId, int minimumSpellLevel)
        {
            // Make the legacy version of the spell inaccessable by removing the casting traditions
            ModManager.ReplaceExistingSpell(legacySpellId, minimumSpellLevel, delegate (Creature? creature, int spellLevel, bool inCombat, SpellInformation spellInformation)
            {
                CombatAction? existingSpell = minimumSpellLevel switch
                {
                    0 => Cantrips.LoadModernSpell(legacySpellId, creature, spellLevel, inCombat, spellInformation),
                    1 => Level1Spells.LoadModernSpell(legacySpellId, creature, spellLevel, inCombat, spellInformation),
                    2 => Level2Spells.LoadModernSpell(legacySpellId, creature, spellLevel, inCombat, spellInformation),
                    _ => null
                } ?? throw new Exception("Invalid Spell: " + legacySpellId);
                IEnumerable<Trait> filteredTraits = existingSpell.Traits.Where((trait) => trait switch
                {
                    Trait.Arcane => false,
                    Trait.Divine => false,
                    Trait.Occult => false,
                    Trait.Primal => false,
                    _ => true
                });
                existingSpell.Traits = new Traits(filteredTraits, existingSpell);
                return existingSpell;
            });
        }
    }
}
