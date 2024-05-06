using System.Reflection;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
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
            public static Core.Mechanics.Enumerations.Trait Remaster = ModManager.RegisterTrait("Remaster");
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
        }

        private static bool initialized = false;
        private static Func<SpellId, SpellId>? GetUpdatedSpellIdFunc;

        [DawnsburyDaysModMainMethod]
        public static void LoadMod()
        {
            if (IsModLoaded("Dawnsbury.Mods.Remaster.Spellbook"))
            {
                if (!initialized)
                {
                    GetUpdatedSpellIdFunc = LoadGetUpdatedSpellIdFunction();
                    AddOrReplaceFeats(Cleric.LoadAll());
                    AddOrReplaceFeats(Fighter.LoadAll());
                    AddOrReplaceFeats(Sorcerer.LoadAll());
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

        public static SpellId GetUpdatedSpellId(SpellId spellId)
        {
            return GetUpdatedSpellIdFunc!(spellId);
        }

        // Tricky code to manage dependencies
        // This mod requires the RemasterSpells mod, and wants to be initialized after it
        private static bool IsModLoaded(string assemblyName)
        {
            return AppDomain.CurrentDomain.GetAssemblies().Any((Assembly assembly) => assembly.GetName().Name == assemblyName);
        }

        private static Func<SpellId, SpellId> LoadGetUpdatedSpellIdFunction()
        {
            Assembly remasterSpellsAssembly = AppDomain.CurrentDomain.GetAssemblies().First((Assembly assembly) => assembly.GetName().Name == "Dawnsbury.Mods.Remaster.Spellbook");
            Type? remasterSpellsClass = remasterSpellsAssembly.GetType("Dawnsbury.Mods.Remaster.Spellbook.RemasterSpells");
            MethodInfo? methodInfo = remasterSpellsClass?.GetMethod("GetUpdatedSpellId", BindingFlags.Static | BindingFlags.Public, new Type[] {typeof(SpellId)});
            if (methodInfo == null)
            {
                if (remasterSpellsClass == null)
                {
                    throw new Exception("Missing RemasterSpells class. Check to see that you have the proper version of that mod installed.");
                }
                throw new Exception("Missing RemasterSpells.GetUpdatedSpellId function. Check to see that you have the proper version of that mod installed.");
            }
            return (Func<SpellId, SpellId>)Delegate.CreateDelegate(typeof(Func<SpellId, SpellId>), methodInfo);
        }
    }
}
