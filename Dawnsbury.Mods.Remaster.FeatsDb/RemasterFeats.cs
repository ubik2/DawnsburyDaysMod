using System.Reflection;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.IO;
using Dawnsbury.Modding;
using Dawnsbury.Mods.Remaster.FeatsDb.TrueFeatsDb;

namespace Dawnsbury.Mods.Remaster.FeatsDb
{

    public class RemasterFeats
    {
        public class Trait
        {
            // We do this in both this mod and the RemasterSpells mod, but it's fine to register the same trait more than once.
            public static readonly Core.Mechanics.Enumerations.Trait Remaster = ModManager.RegisterTrait("Remaster");

            // Traits to represent the various Wizard Curriculum options
            public static readonly Core.Mechanics.Enumerations.Trait ArsGrammatica = ModManager.RegisterTrait("SchoolArsGrammatica", new TraitProperties("Ars Grammatica", false));
            public static readonly Core.Mechanics.Enumerations.Trait BattleMagic = ModManager.RegisterTrait("SchoolBattleMagic", new TraitProperties("Battle Magic", false));
            public static readonly Core.Mechanics.Enumerations.Trait CivicWizardry = ModManager.RegisterTrait("SchoolCivicWizardry", new TraitProperties("Civic Wizardry", false));
            public static readonly Core.Mechanics.Enumerations.Trait Mentalism = ModManager.RegisterTrait("SchoolMentalism", new TraitProperties("Mentalism", false));
            public static readonly Core.Mechanics.Enumerations.Trait ProteanForm = ModManager.RegisterTrait("SchoolProteanForm", new TraitProperties("Protean Form", false));
            public static readonly Core.Mechanics.Enumerations.Trait TheBoundary = ModManager.RegisterTrait("SchoolTheBoundary", new TraitProperties("The Boundary", false));
            public static readonly Core.Mechanics.Enumerations.Trait UnifiedMagicalTheory = ModManager.RegisterTrait("SchoolUnifiedMagicalTheory");
        }

        public class FeatName
        {
            // Cleric - we also have the deity selection feats and the fonds, which aren't included here
            public static readonly Core.CharacterBuilder.Feats.FeatName DivineCastigation = ModManager.RegisterFeatName("DivineCastigation", "Divine Castigation");
            public static readonly Core.CharacterBuilder.Feats.FeatName PanicTheDead = ModManager.RegisterFeatName("PanicTheDead", "Panic the Dead");
            public static readonly Core.CharacterBuilder.Feats.FeatName WarpriestsArmor = ModManager.RegisterFeatName("WarpriestsArmor", "Warpriest's Armor");
            public static readonly Core.CharacterBuilder.Feats.FeatName ChannelSmite = ModManager.RegisterFeatName("ChannelSmite", "Channel Smite");
            public static readonly Core.CharacterBuilder.Feats.FeatName RaiseSymbol = ModManager.RegisterFeatName("RaiseSymbol", "Raise Symbol");

            // Fighter
            public static readonly Core.CharacterBuilder.Feats.FeatName ViciousSwing = ModManager.RegisterFeatName("ViciousSwing", "Vicious Swing");
            public static readonly Core.CharacterBuilder.Feats.FeatName SlamDown = ModManager.RegisterFeatName("SlamDown", "Slam Down");

            // Wizard Curricula
            public static readonly Core.CharacterBuilder.Feats.FeatName ArsGrammatica = ModManager.RegisterFeatName("ArsGrammatica", "School of Ars Grammatica");
            public static readonly Core.CharacterBuilder.Feats.FeatName BattleMagic = ModManager.RegisterFeatName("BattleMagic", "School of Battle Magic");
            public static readonly Core.CharacterBuilder.Feats.FeatName CivicWizardry = ModManager.RegisterFeatName("CivicWizardry", "School of Civic Wizardry");
            public static readonly Core.CharacterBuilder.Feats.FeatName Mentalism = ModManager.RegisterFeatName("Mentalism", "School of Mentalism");
            public static readonly Core.CharacterBuilder.Feats.FeatName ProteanForm = ModManager.RegisterFeatName("ProteanForm", "School of Protean Form");
            public static readonly Core.CharacterBuilder.Feats.FeatName TheBoundary = ModManager.RegisterFeatName("TheBoundary", "School of the Boundary");
            public static readonly Core.CharacterBuilder.Feats.FeatName UnifiedMagicalTheory = ModManager.RegisterFeatName("UnifiedMagicalTheory", "School of Unified Magical Theory");
        }

        private static bool initialized = false;
        private static Func<SpellId, SpellId>? GetUpdatedSpellIdFunc;
        private static Func<string, SpellId>? GetSpellIdByNameFunc;

        [DawnsburyDaysModMainMethod]
        public static void LoadMod()
        {
            if (IsModLoaded("Dawnsbury.Mods.Remaster.Spellbook"))
            {
                if (!initialized)
                {
                    SetupSpellLookupFunctions();
                    AddOrReplaceFeats(Cleric.LoadAll());
                    AddOrReplaceFeats(Fighter.LoadAll());
                    AddOrReplaceFeats(Rogue.LoadAll());
                    AddOrReplaceFeats(Sorcerer.LoadAll());
                    AddOrReplaceFeats(Wizard.LoadAll());
                    initialized = true;
                    GeneralLog.Log("Loaded RemasterFeats mod.");
                }
            }
            else
            {
                GeneralLog.Log("Deferring RemasterFeats mod until after RemasterSpells. If that mod is not available, this mod will not activate.");
            }
        }

        public static void AddOrReplaceFeats(IEnumerable<Feat> feats)
        {
            foreach (var feat in feats)
            {
                // Remove any feats that have the same name as one of our new feats
                AllFeats.All.RemoveAll((existingFeat) => existingFeat.FeatName == feat.FeatName);
                if (!feat.HasTrait(Trait.Remaster))
                {
                    feat.Traits.Add(Trait.Remaster);
                }
                ModManager.AddFeat(feat);
            }
        }

        // Look up a spell's replacement, or the original if it hasn't been replaced
        public static SpellId GetUpdatedSpellId(SpellId spellId)
        {
            return GetUpdatedSpellIdFunc!(spellId);
        }

        // Look up a spell by name
        public static SpellId GetSpellIdByName(string spellName)
        {
            return GetSpellIdByNameFunc!(spellName);
        }

        // Tricky code to manage dependencies
        // This mod requires the RemasterSpells mod, and wants to be initialized after it
        private static bool IsModLoaded(string assemblyName)
        {
            return AppDomain.CurrentDomain.GetAssemblies().Any((Assembly assembly) => assembly.GetName().Name == assemblyName);
        }

        private static void SetupSpellLookupFunctions()
        {
            Assembly remasterSpellsAssembly = AppDomain.CurrentDomain.GetAssemblies().First((Assembly assembly) => assembly.GetName().Name == "Dawnsbury.Mods.Remaster.Spellbook");
            Type? remasterSpellsClass = remasterSpellsAssembly.GetType("Dawnsbury.Mods.Remaster.Spellbook.RemasterSpells");
            if (remasterSpellsClass == null)
            {
                throw new Exception("Missing RemasterSpells class. Check to see that you have the proper version of that mod installed.");
            }
            MethodInfo? methodInfo = remasterSpellsClass?.GetMethod("GetUpdatedSpellId", BindingFlags.Static | BindingFlags.Public, new Type[] { typeof(SpellId) });
            if (methodInfo == null)
            {
                throw new Exception("Missing RemasterSpells.GetUpdatedSpellId function. Check to see that you have the proper version of that mod installed.");
            }
            GetUpdatedSpellIdFunc = (Func<SpellId, SpellId>)Delegate.CreateDelegate(typeof(Func<SpellId, SpellId>), methodInfo);

            methodInfo = remasterSpellsClass?.GetMethod("GetSpellIdByName", BindingFlags.Static | BindingFlags.Public, new Type[] { typeof(string) });
            if (methodInfo == null)
            {
                throw new Exception("Missing RemasterSpells.GetSpellIdByName function. Check to see that you have the proper version of that mod installed.");
            }
            GetSpellIdByNameFunc = (Func<string, SpellId>)Delegate.CreateDelegate(typeof(Func<string, SpellId>), methodInfo);
        }
    }
}
