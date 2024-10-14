using Dawnsbury.Modding;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.IO;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using System.Reflection;
using System.Text.RegularExpressions;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Core.Mechanics.Targeting;

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
            try
            {
                SpellId spellId = ModManager.RegisterNewSpell(technicalSpellName, minimumSpellLevel, createSpellInstance);
                newSpells.Add(technicalSpellName, spellId);
                return spellId;
            }
            catch (ArgumentException ex)
            {
                GeneralLog.Log("Skipped registering new spell \"" + technicalSpellName + "\". This spell may already be provided by another mod.\n" + ex.ToString());
                return SpellId.None;
            }
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

        /// <summary>
        ///  This is a utility function that attaches an effect to the caster. This effect provides an action possibility that lets the caster end 
        ///  the spell early, and also cleans up any subeffects when it expires.
        /// </summary>
        /// <param name="caster"></param>
        /// <param name="spell"></param>
        /// <param name="subEffects"></param>
        /// <param name="tileEffects"></param>
        /// <returns></returns>
        internal static QEffect CreateDismissAction(Creature caster, CombatAction spell, List<QEffect>? subEffects, List<TileQEffect>? tileEffects)
        {
            QEffect dismissableEffect = new QEffect()
            {
                ProvideActionIntoPossibilitySection = (QEffect effect, PossibilitySection section) => (section.PossibilitySectionId != PossibilitySectionId.OtherManeuvers) ?
                    null : new ActionPossibility(new CombatAction(caster, spell.Illustration, "Dismiss " + spell.Name, [Core.Mechanics.Enumerations.Trait.Concentrate], "Dismiss this effect.", Target.Self())
                .WithEffectOnSelf((_) =>
                {
                    // This should also remove the effect that gave us the option to dismiss the spell
                    effect.ExpiresAt = ExpirationCondition.Immediately;
                })),
                WhenExpires = (_) =>
                {
                    if (subEffects != null)
                    {
                        foreach (QEffect subEffect in subEffects)
                        {
                            subEffect.ExpiresAt = ExpirationCondition.Immediately;
                        }
                    }
                    if (tileEffects != null)
                    {
                        foreach (TileQEffect tileEffect in tileEffects)
                        {
                            tileEffect.ExpiresAt = ExpirationCondition.Immediately;
                        }
                    }
                }
            };
            caster.AddQEffect(dismissableEffect);
            return dismissableEffect;
        }

        internal static string StripInitialWhitespace(string str)
        {
            return Regex.Replace(str, @"^\n*", "");
        }
    }
}
