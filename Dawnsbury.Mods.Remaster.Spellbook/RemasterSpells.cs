using Dawnsbury.Modding;
using Dawnsbury.Core.Mechanics.Enumerations;

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
            // These are aliases. We'll rename them below.
            public static Core.Mechanics.Enumerations.Trait Vitality = Core.Mechanics.Enumerations.Trait.Positive;
            public static Core.Mechanics.Enumerations.Trait Void = Core.Mechanics.Enumerations.Trait.Negative;
        }

        [DawnsburyDaysModMainMethod]
        public static void LoadMod()
        {
            Cantrips.RegisterSpells();
            Level1Spells.RegisterSpells();
            FocusSpells.RegisterSpells();
            RenameTrait(Core.Mechanics.Enumerations.Trait.Positive, "Vitality");
            RenameTrait(Core.Mechanics.Enumerations.Trait.Negative, "Void");
        }

        static void RenameTrait(Core.Mechanics.Enumerations.Trait trait, string newName)
        {
            var properties = TraitExtensions.TraitProperties[trait];
            TraitExtensions.TraitProperties[trait] = new TraitProperties(newName, properties.Relevant, properties.RulesText, properties.RelevantForShortBlock); 
        }        
    }
}
