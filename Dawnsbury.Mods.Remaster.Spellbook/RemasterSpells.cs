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
            FocusSpells.RegisterSpells();
            Level1Spells.RegisterSpells();
            Level2Spells.RegisterSpells();
            RenameTrait(Core.Mechanics.Enumerations.Trait.Positive, "Vitality");
            RenameTrait(Core.Mechanics.Enumerations.Trait.Negative, "Void");
        }

        static void RenameTrait(Core.Mechanics.Enumerations.Trait trait, string newName)
        {
            // It looks like the timing of these trait properties doesn't work the same as a workshop mod
            if (TraitExtensions.TraitProperties.TryGetValue(trait, out TraitProperties? properties))
            {
                TraitExtensions.TraitProperties[trait] = new TraitProperties(newName, properties.Relevant, properties.RulesText, properties.RelevantForShortBlock);
            }
        }        
    }
}
