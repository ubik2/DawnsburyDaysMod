using Dawnsbury.Modding;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.IO;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Dawnsbury.Mods.Remaster.Spellbook
{

    public class RemasterSpells
    {
        public class Trait
        {
            public static readonly Core.Mechanics.Enumerations.Trait Remaster = ModManager.RegisterTrait("Remaster");
            public static readonly Core.Mechanics.Enumerations.Trait Sanctified = ModManager.RegisterTrait("Sanctified");
            public static readonly Core.Mechanics.Enumerations.Trait Spirit = ModManager.RegisterTrait("Spirit");
            public static readonly Core.Mechanics.Enumerations.Trait Disease = ModManager.RegisterTrait("Disease");
            public static readonly Core.Mechanics.Enumerations.Trait Revelation = ModManager.RegisterTrait("Revelation");
            // These are aliases. We'll rename them below.
            public static readonly Core.Mechanics.Enumerations.Trait Vitality = Core.Mechanics.Enumerations.Trait.Positive;
            public static readonly Core.Mechanics.Enumerations.Trait Void = Core.Mechanics.Enumerations.Trait.Negative;
            // New trait for summon spells
            public static readonly Core.Mechanics.Enumerations.Trait Summon = ModManager.RegisterTrait("Summon");
        }

        private static readonly Dictionary<string, SpellId> newSpells = new Dictionary<string, SpellId>();
        private static readonly Dictionary<SpellId, SpellId> replacementSpells = new Dictionary<SpellId, SpellId>();

        private static bool initialized = false;
        
        [DawnsburyDaysModMainMethod]
        public static void LoadMod()
        {
            // Should be single threaded here, so we can just use a variable.
            if (!initialized)
            {
                Cantrips.RegisterSpells();
                FocusSpells.RegisterSpells();
                Level1Spells.RegisterSpells();
                Level2Spells.RegisterSpells();
                RenameTrait(Core.Mechanics.Enumerations.Trait.Positive, "Vitality");
                RenameTrait(Core.Mechanics.Enumerations.Trait.Negative, "Void");
                initialized = true;
                GeneralLog.Log("Loaded RemasterSpells mod");

                // Our featsdb mod requires this mod to be loaded, so it will have skipped its load functionality.
                // Now that we're loaded, we can reload that mod.
                if (TryLoadDeferredMod("Dawnsbury.Mods.Remaster.FeatsDb"))
                {
                    GeneralLog.Log("Loaded deferred mod: " + "Dawnsbury.Mods.Remaster.FeatsDb");
                }
            }
        }

        static void RenameTrait(Core.Mechanics.Enumerations.Trait trait, string newName)
        {
            TraitProperties? properties = TraitExtensions.GetTraitProperties(trait);
            TraitExtensions.TraitProperties[trait] = new TraitProperties(newName, properties.Relevant, properties.RulesText, properties.RelevantForShortBlock);
        }

        public static SpellId ReplaceLegacySpell(SpellId legacySpellId, string remasterName, int minimumSpellLevel, Func<SpellId, Creature?, int, bool, SpellInformation, CombatAction> createSpellInstance)
        {
            SpellId spellId = ModManager.RegisterNewSpell(remasterName, minimumSpellLevel, createSpellInstance);
            newSpells.Add(remasterName, spellId);
            replacementSpells.Add(legacySpellId, spellId);
            return spellId;
        }

        public static SpellId RegisterNewSpell(string technicalSpellName, int minimumSpellLevel, Func<SpellId, Creature?, int, bool, SpellInformation, CombatAction> createSpellInstance)
        {
            SpellId spellId = ModManager.RegisterNewSpell(technicalSpellName, minimumSpellLevel, createSpellInstance);
            newSpells.Add(technicalSpellName, spellId);
            return spellId;
        }

        /// <remarks>
        /// <see cref="RemasterSpells.GetUpdatedSpellId(SpellId)"/> is called via reflection.
        /// </remarks>
        public static SpellId GetUpdatedSpellId(SpellId spellId)
        {
            if (replacementSpells.TryGetValue(spellId, out SpellId replacementSpellId))
            {
                return replacementSpellId;
            }
            else
            {
                return spellId;
            }
        }

        /// <remarks>
        /// <see cref="RemasterSpells.GetSpellIdByName(string)"/> is called via reflection.
        /// </remarks>
        public static SpellId GetSpellIdByName(string spellName)
        {
            if (newSpells.TryGetValue(spellName, out SpellId spellId))
            {
                return spellId;
            }
            else
            {
                throw new InvalidOperationException("The spell name was not found");
            }
        }

        // Tricky code to handle module dependencies.
        // Specifically, the feat 

        private static Assembly? GetMod(string assemblyName)
        {
            return AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault((Assembly assembly) => assembly.GetName().Name == assemblyName);
        }

        private static bool TryLoadDeferredMod(string modName)
        {
            // Are they also using the specified mod? If so, we can load that now (it will have been deferred if we tried to load it first).
            Assembly? featsMod = GetMod(modName);
            bool foundMethod = false;
            if (featsMod != null)
            {
                foreach (Type type in featsMod.GetTypes())
                {
                    foreach (MethodInfo method in type.GetMethods())
                    {
                        if (method.GetCustomAttribute<DawnsburyDaysModMainMethodAttribute>() != null)
                        {
                            _ = method.Invoke(null, BindingFlags.Static | BindingFlags.Public, null, null, null);
                            foundMethod = true;
                        }
                    }
                }
            }
            return foundMethod;
        }

        internal static string StripInitialWhitespace(string str)
        {
            return Regex.Replace(str, @"^\n*", "");
        }
    }
}
