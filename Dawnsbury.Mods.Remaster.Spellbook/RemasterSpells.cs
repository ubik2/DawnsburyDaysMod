using Dawnsbury.Modding;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dawnsbury.Mods.Remaster.Spellbook
{

    public class RemasterSpells
    {
        public class Trait
        {
            public static Core.Mechanics.Enumerations.Trait Remaster = ModManager.RegisterTrait("Remaster");
            public static Core.Mechanics.Enumerations.Trait Sanctified = ModManager.RegisterTrait("Sanctified");
            public static Core.Mechanics.Enumerations.Trait Spirit = ModManager.RegisterTrait("Spirit");
            public static Core.Mechanics.Enumerations.Trait Disease = ModManager.RegisterTrait("Disease");
            public static Core.Mechanics.Enumerations.Trait Revelation = ModManager.RegisterTrait("Revelation");
            // These are aliases. We'll rename them below.
            public static Core.Mechanics.Enumerations.Trait Vitality = Core.Mechanics.Enumerations.Trait.Positive;
            public static Core.Mechanics.Enumerations.Trait Void = Core.Mechanics.Enumerations.Trait.Negative;
        }

        [DawnsburyDaysModMainMethod]
        public static void LoadMod()
        {
            Cantrips.RegisterSpells();
            FocusSpells.RegisterSpells();
            Level1Spells.RegisterSpells();
            Level2Spells.RegisterSpells();
            RenameTrait(Core.Mechanics.Enumerations.Trait.Positive, "Vitality");
            RenameTrait(Core.Mechanics.Enumerations.Trait.Negative, "Void");
        }

        static void RenameTrait(Core.Mechanics.Enumerations.Trait trait, string newName)
        {
            TraitProperties? properties = TraitExtensions.GetTraitProperties(trait);
            TraitExtensions.TraitProperties[trait] = new TraitProperties(newName, properties.Relevant, properties.RulesText, properties.RelevantForShortBlock);
        }
        public static SpellId ReplaceLegacySpell(SpellId legacySpellId, string remasterName, int minimumSpellLevel, Func<SpellId, Creature?, int, bool, SpellInformation, CombatAction> createSpellInstance)
        {
            if (legacySpellId.ToString() == remasterName)
            {
                throw new ArgumentException("Unable to replace a legacy spell with another spell with the same name.");
            }
#if HIDE_LEGACY
            // Make the legacy version of the spell inaccessable by removing the casting traditions
            ModManager.ReplaceExistingSpell(legacySpellId, minimumSpellLevel, delegate (Creature? creature, int spellLevel, bool inCombat, SpellInformation spellInformation)
            {
                CombatAction? existingSpell = minimumSpellLevel switch
                {
                    0 => Core.CharacterBuilder.FeatsDb.Spellbook.Cantrips.LoadModernSpell(legacySpellId, creature, spellLevel, inCombat, spellInformation),
                    1 => Core.CharacterBuilder.FeatsDb.Spellbook.Level1Spells.LoadModernSpell(legacySpellId, creature, spellLevel, inCombat, spellInformation),
                    2 => Core.CharacterBuilder.FeatsDb.Spellbook.Level2Spells.LoadModernSpell(legacySpellId, creature, spellLevel, inCombat, spellInformation),
                    _ => null
                };
                if (existingSpell == null)
                {
                    throw new Exception("Invalid Spell: " + legacySpellId);
                }
                IEnumerable<Core.Mechanics.Enumerations.Trait> filteredTraits = existingSpell.Traits.Where((trait) => trait switch {
                    Core.Mechanics.Enumerations.Trait.Arcane => false,
                    Core.Mechanics.Enumerations.Trait.Divine => false,
                    Core.Mechanics.Enumerations.Trait.Occult => false,
                    Core.Mechanics.Enumerations.Trait.Primal => false,
                    _ => true
                });
                existingSpell.Traits = new Traits(filteredTraits, existingSpell);
                return existingSpell;
            });
#endif
            return ModManager.RegisterNewSpell(remasterName, minimumSpellLevel, createSpellInstance);
        }
    }
}
