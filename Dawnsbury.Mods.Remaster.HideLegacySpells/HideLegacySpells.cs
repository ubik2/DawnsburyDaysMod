using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Modding;
using System;
using System.Linq;

namespace Dawnsbury.Mods.Remaster.HideLegacySpells
{
    public class HideLegacySpells
    {
        [DawnsburyDaysModMainMethod]
        public static void LoadMod()
        {
            // A list of legacy spell ids for each level (starting at 0)
            SpellId[] legacySpells = new[] {
               SpellId.AcidSplash, SpellId.RayOfFrost, SpellId.ProduceFlame, SpellId.DisruptUndead, SpellId.ChillTouch,
               SpellId.BurningHands, SpellId.ColorSpray, SpellId.MagicMissile, SpellId.MageArmor, SpellId.MagicWeapon, SpellId.TrueStrike, SpellId.ShockingGrasp,
               SpellId.AcidArrow, SpellId.CalmEmotions, SpellId.FlamingSphere, SpellId.HideousLaughter, SpellId.ObscuringMist, SpellId.SoundBurst, SpellId.Barkskin, SpellId.SpiritualWeapon, SpellId.TouchOfIdiocy
            };
            ModManager.RegisterActionOnEachSpell((spell) =>
            {
                if (legacySpells.Contains(spell.SpellId))
                {
                    spell.Traits.Add(Trait.SpellCannotBeChosenInCharacterBuilder);
                }
            });
        }
    }
}
