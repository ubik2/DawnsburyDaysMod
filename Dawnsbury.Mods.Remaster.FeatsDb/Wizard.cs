

using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.Remaster.FeatsDb
{
    public static class Wizard
    {
        public static IEnumerable<Feat> LoadAll()
        {
            // Add traits to our spells to associated then with a curriculum
            PatchWizardSpells();

            // Generate the curriculum feats
            foreach (Feat curriculumFeat in LoadCurricula())
            {
                yield return curriculumFeat;
            }

            // Update the wizard class to select from the curricula instead of the old spell schools
            PatchWizard();
        }

        public static IEnumerable<Feat> LoadCurricula()
        {
            // TODO: I include the spell options here, but it's really the PatchWizard where we add the traits to the spells that's responsible.
            yield return new CurriculumFeat(RemasterFeats.FeatName.ArsGrammatica, RemasterFeats.Trait.ArsGrammatica,
                "Runes and wards, numbers and letters—they underpin all magic, making them the logical subject for a wizard who studies fundamental forces. Perhaps you studied at the Pathfinder Society's School of Spells or a similar institution, but whether you're lacing your words with magic to compel others, casting wards around your workshop, or destabilizing the very structure of an opponent's spells, you know this unassuming school carries elegant power.",
                RemasterFeats.GetSpellIdByName("ProtectiveWards"),
                new Dictionary<int, SpellId[]>()
                {
                    { 0, new[] { SpellId.Daze } }, // Message, Sigil; Daze doesn't belong.
                    { 1, new[] { SpellId.Command, RemasterFeats.GetSpellIdByName("RunicBody"), RemasterFeats.GetSpellIdByName("RunicWeapon") } }, // Disguise Magic
                    { 2, Array.Empty<SpellId>() }, // Dispel Magic, Translate
                });

            yield return new CurriculumFeat(RemasterFeats.FeatName.BattleMagic, RemasterFeats.Trait.BattleMagic,
                "Runes and wards, numbers and letters—they underpin all magic, making them the logical subject for a wizard who studies fundamental forces. Perhaps you studied at the Pathfinder Society's School of Spells or a similar institution, but whether you're lacing your words with magic to compel others, casting wards around your workshop, or destabilizing the very structure of an opponent's spells, you know this unassuming school carries elegant power.",
                SpellId.ForceBolt,
                new Dictionary<int, SpellId[]>()
                {
                    { 0, new[] { SpellId.Shield, SpellId.TelekineticProjectile } },
                    { 1, new[] { RemasterFeats.GetSpellIdByName("BreatheFire"), RemasterFeats.GetSpellIdByName("ForceBarrage"), RemasterFeats.GetSpellIdByName("MysticArmor") } },
                    { 2, new[] { RemasterFeats.GetSpellIdByName("Mist"), SpellId.ResistEnergy } }
                });

            yield return new CurriculumFeat(RemasterFeats.FeatName.CivicWizardry, RemasterFeats.Trait.CivicWizardry,
                "Whether you studied in Manaket's Occularium or the Academy of Applied Magic, you learned that the fruits of arcane studies—like any other field—should ultimately help the common citizen. You've learned the humble art of construction, of finding lost people and things, of moving speedily among buildings and moats—yet these same arts can be turned to demolition, and the constructs you animate to build bridges can just as easily tear them down.",
                RemasterFeats.GetSpellIdByName("Earthworks"),
                new Dictionary<int, SpellId[]>()
                {
                    { 0, new[] { SpellId.TelekineticProjectile } }, // Prestidigitation, Read Aura; Telekinetic Projectile doesn't belong
                    { 1, new[] { SpellId.HydraulicPush, SpellId.PummelingRubble, RemasterFeats.GetSpellIdByName("SummonConstruct") } },
                    { 2, new[] { RemasterFeats.GetSpellIdByName("RevealingLight") } } // Water Walk
                });

            yield return new CurriculumFeat(RemasterFeats.FeatName.Mentalism, RemasterFeats.Trait.Mentalism,
                "As a scholar, you know all too well the importance of a sound mind. Thus, you attended a school—like the Farseer Tower or the Stone of the Seers—that taught the arts of befuddling lesser minds with figments and illusions or implanted sensations and memories.",
                RemasterFeats.GetSpellIdByName("CharmingPush"),
                new Dictionary<int, SpellId[]>()
                {
                    { 0, new[] { SpellId.Daze } }, // Figment
                    { 1, new[] { RemasterFeats.GetSpellIdByName("DizzyingColors"), RemasterFeats.GetSpellIdByName("SureStrike") } }, // Sleep
                    { 2, new[] { RemasterFeats.GetSpellIdByName("Stupefy") } } // Illusory Creature
                });

            yield return new CurriculumFeat(RemasterFeats.FeatName.ProteanForm, RemasterFeats.Trait.ProteanForm,
                "As a scholar, you know all too well the importance of a sound mind. Thus, you attended a school—like the Farseer Tower or the Stone of the Seers—that taught the arts of befuddling lesser minds with figments and illusions or implanted sensations and memories.",
                RemasterFeats.GetSpellIdByName("ScrambleBody"),
                new Dictionary<int, SpellId[]>()
                {
                    { 0, new[] { RemasterFeats.GetSpellIdByName("TangleVine") } },
                    { 1, new[] { RemasterFeats.GetSpellIdByName("GougingClaw"), SpellId.InsectForm, RemasterFeats.GetSpellIdByName("SpiderSting") } }, // Jump, Pest Form  -> Insect Swarm
                    { 2, Array.Empty<SpellId>() } // Enlarge, Humanoid Form
                });

            yield return new CurriculumFeat(RemasterFeats.FeatName.TheBoundary, RemasterFeats.Trait.TheBoundary,
                "Why use your magic to affect something as pedestrian as the physical world? Whether you studied at the College of Dimensional Studies in Katapesh or an underground school in haunted Ustalav, you've turned your magic past the Universe to the forces beyond, summoning spirits and shades, manipulating dimensions and planes, and treading in a place not meant for mortals.",
                RemasterFeats.GetSpellIdByName("FortifySummoning"),
                new Dictionary<int, SpellId[]>()
                {
                    { 0, new[] { RemasterFeats.GetSpellIdByName("VoidWarp") } }, // Telekinetic Hand
                    { 1, new[] { SpellId.GrimTendrils, RemasterFeats.GetSpellIdByName("SummonUndead") } }, // Phantasmal Minion
                    { 2, new[] { RemasterFeats.GetSpellIdByName("SeeTheUnseen") } } // Darkness
                });
        }


        // We need to extend ArcaneSchoolPreparedSpellSlot, since there's a type based check in the character sheet
        public class CurriculumPreparedSpellSlot : ArcaneSchoolPreparedSpellSlot
        {
            public override string SlotName { get; }

            public CurriculumPreparedSpellSlot(int level, string key, Trait school)
                : base(level, key, school)
            {
                SlotName = TraitExtensions.TraitProperties[school].HumanizedName;
            }
        }

        public class CurriculumFeat : Feat
        {
            public CurriculumFeat(FeatName schoolFeat, Trait schoolTrait, string flavorText, SpellId focusSpell, Dictionary<int, SpellId[]> spellOptions)
                : base(schoolFeat, flavorText, "XX", new List<Trait>(), null)
            {
                Spell modernSpellTemplate = AllSpells.CreateModernSpellTemplate(focusSpell, Trait.Wizard);
                RulesText = "You gain an extra spell slot at each spell level for which you have wizard spell slots. You can only prepare spells from the " + Name + " in this slot.\n\nYou learn the " + AllSpells.CreateModernSpellTemplate(focusSpell, Trait.Wizard).ToSpellLink() + " focus school spell and you gain a focus pool of 1 focus point which recharges after every encounter.\n" +"\n{b}Curriculum{/b}";
                if (spellOptions[0].Length > 0)
                {
                    RulesText += "\ncantrips: " + string.Join(", ", spellOptions[0].Select((SpellId id) => AllSpells.CreateModernSpellTemplate(id, Trait.Wizard).ToSpellLink()));
                }
                if (spellOptions[1].Length > 0)
                {
                    RulesText += "\n1st: " + string.Join(", ", spellOptions[1].Select((SpellId id) => AllSpells.CreateModernSpellTemplate(id, Trait.Wizard).ToSpellLink()));
                }
                if (spellOptions[2].Length > 0)
                {
                    RulesText += "\n2nd: " + string.Join(", ", spellOptions[2].Select((SpellId id) => AllSpells.CreateModernSpellTemplate(id, Trait.Wizard).ToSpellLink()));
                }
                ShowRulesBlockForClassOfOrigin = Trait.Wizard; 
                OnSheet = (sheet) =>
                {
                    if (schoolTrait != RemasterFeats.Trait.UnifiedMagicalTheory)
                    {
                        sheet.WizardSchool = schoolTrait; // Create new traits for each curriculum
                        sheet.PreparedSpells.GetValueOrDefault(Trait.Wizard)?.Slots.Add(new CurriculumPreparedSpellSlot(0, "Wizard:SchoolSpell0:" + schoolTrait.ToString(), schoolTrait));
                        sheet.PreparedSpells.GetValueOrDefault(Trait.Wizard)?.Slots.Add(new CurriculumPreparedSpellSlot(1, "Wizard:SchoolSpell1:" + schoolTrait.ToString(), schoolTrait));
                        sheet.AddAtLevel(3, (laterValues) => laterValues.PreparedSpells.GetValueOrDefault(Trait.Wizard)?.Slots.Add(new CurriculumPreparedSpellSlot(2, "Wizard:SchoolSpell2:" + schoolTrait.ToString(), schoolTrait)));
                    }
                    // FIXME: need to add upgraded version of Drain Bonded Item and bonus feat for Unified Magical Theory
                    sheet.AddFocusSpellAndFocusPoint(Trait.Wizard, Ability.Intelligence, focusSpell);
                };
                Illustration = modernSpellTemplate.Illustration; // we just use the illustration from our focus spell
            }
        }

        private static void PatchWizard()
        {
            ClassSelectionFeat classFeat = (ClassSelectionFeat)AllFeats.All.First((feat) => feat.FeatName == FeatName.Wizard);
            // Grant trained with simple weapons
            classFeat.RulesText = classFeat.RulesText.Replace("You're trained in the club, crossbow, dagger, heavy crossbow and the staff.", "You're trained in all simple weapons.");
            classFeat.OnSheet = (Action<CalculatedCharacterSheetValues>)Delegate.Combine(classFeat.OnSheet, (CalculatedCharacterSheetValues sheet) => sheet.SetProficiency(Trait.Simple, Proficiency.Trained));
            // TODO: Replace old school specializations with new versions
            classFeat.Subfeats = AllFeats.All.Where((feat) => feat.FeatName == FeatName.UniversalistSchool || feat is CurriculumFeat).ToList();
        }

        private static void PatchWizardSpells()
        {
            // We add traits to the non-cantrips so we get the green highlight effect in spell selection
            Dictionary<SpellId, Trait[]> schoolTraitsMap = new Dictionary<SpellId, Trait[]> {
                { SpellId.Daze, new[] { RemasterFeats.Trait.ArsGrammatica, RemasterFeats.Trait.Mentalism } },
                { SpellId.Shield, new[] { RemasterFeats.Trait.BattleMagic } },
                { SpellId.TelekineticProjectile, new[] { RemasterFeats.Trait.BattleMagic, RemasterFeats.Trait.CivicWizardry } },
                { SpellId.Command, new[] { RemasterFeats.Trait.ArsGrammatica } },
                { RemasterFeats.GetSpellIdByName("TangleVine"), new[] { RemasterFeats.Trait.ProteanForm } },
                { RemasterFeats.GetSpellIdByName("VoidWarp"), new[] { RemasterFeats.Trait.TheBoundary } },

                { RemasterFeats.GetSpellIdByName("BreatheFire"), new[] { RemasterFeats.Trait.BattleMagic } },
                { RemasterFeats.GetSpellIdByName("DizzyingColors"), new[] { RemasterFeats.Trait.Mentalism } },
                { RemasterFeats.GetSpellIdByName("ForceBarrage"), new[] { RemasterFeats.Trait.BattleMagic } },
                { SpellId.InsectForm, new[] { RemasterFeats.Trait.ProteanForm } },
                { RemasterFeats.GetSpellIdByName("GougingClaw"), new[] { RemasterFeats.Trait.ProteanForm } },
                { SpellId.GrimTendrils, new[] { RemasterFeats.Trait.TheBoundary } },
                { SpellId.HydraulicPush, new[] { RemasterFeats.Trait.CivicWizardry } },
                { RemasterFeats.GetSpellIdByName("MysticArmor"), new[] { RemasterFeats.Trait.BattleMagic } },
                { SpellId.PummelingRubble, new[] { RemasterFeats.Trait.CivicWizardry } },
                { RemasterFeats.GetSpellIdByName("RunicBody"), new[] { RemasterFeats.Trait.ArsGrammatica } },
                { RemasterFeats.GetSpellIdByName("RunicWeapon"), new[] { RemasterFeats.Trait.ArsGrammatica } },
                { RemasterFeats.GetSpellIdByName("SpiderSting"), new[] { RemasterFeats.Trait.ProteanForm } },
                { RemasterFeats.GetSpellIdByName("SummonConstruct"), new[] { RemasterFeats.Trait.CivicWizardry } },
                { RemasterFeats.GetSpellIdByName("SummonUndead"), new[] { RemasterFeats.Trait.TheBoundary } },
                { RemasterFeats.GetSpellIdByName("SureStrike"), new[] { RemasterFeats.Trait.Mentalism } },

                { RemasterFeats.GetSpellIdByName("Mist"), new[] { RemasterFeats.Trait.BattleMagic } },
                { SpellId.ResistEnergy, new[] { RemasterFeats.Trait.BattleMagic } },
                { RemasterFeats.GetSpellIdByName("RevealingLight"), new[] { RemasterFeats.Trait.CivicWizardry } },
                { RemasterFeats.GetSpellIdByName("Stupefy"), new[] { RemasterFeats.Trait.Mentalism } },
                { RemasterFeats.GetSpellIdByName("SeeTheUnseen"), new[] { RemasterFeats.Trait.TheBoundary } }
            };

            ModManager.RegisterActionOnEachSpell((spell) =>
            {
                if (schoolTraitsMap.TryGetValue(spell.SpellId, out Trait[]? schoolTraits))
                {
                    spell.Traits.AddRange(schoolTraits);
                }
            });
        }
    }
}
