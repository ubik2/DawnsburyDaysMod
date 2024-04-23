using Dawnsbury.Modding;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using System;

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
        public static SpellId ReplaceLegacySpell(SpellId LegacySpellId, string remasterName, int minimumSpellLevel, Func<SpellId, Creature?, int, bool, SpellInformation, CombatAction> createSpellInstance)
        {
            return ModManager.RegisterNewSpell(remasterName, minimumSpellLevel, createSpellInstance);
        }
    }
}
