

using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Modding;
using Dawnsbury.Core.CombatActions;

namespace Dawnsbury.Mods.Remaster.FeatsDb
{
    // * Explosive Arrival
    // * Irresistable Magic (from Spell Penetration)
    
    // * Advanced School Spell (will be different)
    // * Bond Conservation - essentially the same
    // * Knowledge is Power - skipping, since we don't do Recall Knowledge checks

    public static class Wizard
    {
        private static readonly Dictionary<Trait, SpellId[][]> curriculaSpellOptions = new Dictionary<Trait, SpellId[][]>()
        {
            {
                RemasterFeats.Trait.ArsGrammatica,
                [
                    [SpellId.Daze], // Message, Sigil; Daze doesn't belong.
                    [SpellId.Command, RemasterFeats.GetSpellIdByName("RunicBody"), RemasterFeats.GetSpellIdByName("RunicWeapon")], // Disguise Magic
                    [], // Dispel Magic, Translate
                    [], // Enthrall, Veil of Prophecy
                    [], // Dispelling Globe, Suggestion
                ]
            },
            {
                RemasterFeats.Trait.BattleMagic,
                [
                    [SpellId.Shield, SpellId.TelekineticProjectile],
                    [RemasterFeats.GetSpellIdByName("BreatheFire"), RemasterFeats.GetSpellIdByName("ForceBarrage"), RemasterFeats.GetSpellIdByName("MysticArmor")],
                    [RemasterFeats.GetSpellIdByName("Mist"), SpellId.ResistEnergy],
                    [SpellId.Fireball], // Earthbind
                    [], // Wall of Fire, Weapon Storm
                ]
            },
            {
                RemasterFeats.Trait.CivicWizardry,
                [
                    [SpellId.TelekineticProjectile], // Prestidigitation, Read Aura; Telekinetic Projectile doesn't belong
                    [SpellId.HydraulicPush, SpellId.PummelingRubble, RemasterFeats.GetSpellIdByName("SummonConstruct")],
                    [RemasterFeats.GetSpellIdByName("RevealingLight")], // Water Walk
                    [], // Cozy Cabin, Safe Passage
                    [], // Creation, Unfettered Movement
                ]
            },
            {
                RemasterFeats.Trait.Mentalism,
                [
                    [SpellId.Daze], // Figment
                    [RemasterFeats.GetSpellIdByName("DizzyingColors"), RemasterFeats.GetSpellIdByName("SureStrike")], // Sleep
                    [RemasterFeats.GetSpellIdByName("Stupefy")], // Illusory Creature
                    [], // Dream Message, Mind Reading
                    [], // Nightmare, Vision of Death
                ]
            },
            {
                RemasterFeats.Trait.ProteanForm,
                [
                    [RemasterFeats.GetSpellIdByName("TangleVine")],
                    [RemasterFeats.GetSpellIdByName("GougingClaw"), SpellId.InsectForm, RemasterFeats.GetSpellIdByName("SpiderSting")], // Jump, Pest Form  -> Insect Swarm
                    [], // Enlarge, Humanoid Form
                    [], // Feet to Fins, Vampiric Feast
                    [], // Mountain Resilience, Vapor Form
                ]
            },
            {
                RemasterFeats.Trait.TheBoundary,
                [
                    [RemasterFeats.GetSpellIdByName("VoidWarp")], // Telekinetic Hand
                    [SpellId.GrimTendrils, RemasterFeats.GetSpellIdByName("SummonUndead")], // Phantasmal Minion
                    [RemasterFeats.GetSpellIdByName("SeeTheUnseen")], // Darkness
#if V3
                    [SpellId.BindUndead], // Ghostly Weapon
#else
                    [],
#endif
                    [], // Flicker, Translocate
                ]
            }
        };

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
                RemasterFeats.GetSpellIdByName("ProtectiveWards"), curriculaSpellOptions[RemasterFeats.Trait.ArsGrammatica]);

            yield return new CurriculumFeat(RemasterFeats.FeatName.BattleMagic, RemasterFeats.Trait.BattleMagic,
                "Runes and wards, numbers and letters—they underpin all magic, making them the logical subject for a wizard who studies fundamental forces. Perhaps you studied at the Pathfinder Society's School of Spells or a similar institution, but whether you're lacing your words with magic to compel others, casting wards around your workshop, or destabilizing the very structure of an opponent's spells, you know this unassuming school carries elegant power.",
                SpellId.ForceBolt, curriculaSpellOptions[RemasterFeats.Trait.BattleMagic]);

            yield return new CurriculumFeat(RemasterFeats.FeatName.CivicWizardry, RemasterFeats.Trait.CivicWizardry,
                "Whether you studied in Manaket's Occularium or the Academy of Applied Magic, you learned that the fruits of arcane studies—like any other field—should ultimately help the common citizen. You've learned the humble art of construction, of finding lost people and things, of moving speedily among buildings and moats—yet these same arts can be turned to demolition, and the constructs you animate to build bridges can just as easily tear them down.",
                RemasterFeats.GetSpellIdByName("Earthworks"), curriculaSpellOptions[RemasterFeats.Trait.CivicWizardry]);

            yield return new CurriculumFeat(RemasterFeats.FeatName.Mentalism, RemasterFeats.Trait.Mentalism,
                "As a scholar, you know all too well the importance of a sound mind. Thus, you attended a school—like the Farseer Tower or the Stone of the Seers—that taught the arts of befuddling lesser minds with figments and illusions or implanted sensations and memories.",
                RemasterFeats.GetSpellIdByName("CharmingPush"), curriculaSpellOptions[RemasterFeats.Trait.Mentalism]);

            yield return new CurriculumFeat(RemasterFeats.FeatName.ProteanForm, RemasterFeats.Trait.ProteanForm,
                "As a scholar, you know all too well the importance of a sound mind. Thus, you attended a school—like the Farseer Tower or the Stone of the Seers—that taught the arts of befuddling lesser minds with figments and illusions or implanted sensations and memories.",
                RemasterFeats.GetSpellIdByName("ScrambleBody"), curriculaSpellOptions[RemasterFeats.Trait.ProteanForm]);

            yield return new CurriculumFeat(RemasterFeats.FeatName.TheBoundary, RemasterFeats.Trait.TheBoundary,
                "Why use your magic to affect something as pedestrian as the physical world? Whether you studied at the College of Dimensional Studies in Katapesh or an underground school in haunted Ustalav, you've turned your magic past the Universe to the forces beyond, summoning spirits and shades, manipulating dimensions and planes, and treading in a place not meant for mortals.",
                RemasterFeats.GetSpellIdByName("FortifySummoning"), curriculaSpellOptions[RemasterFeats.Trait.TheBoundary]);
        }


        // We need to extend ArcaneSchoolPreparedSpellSlot, since there's a type based check in the character sheet
        public class CurriculumPreparedSpellSlot : ArcaneSchoolPreparedSpellSlot
        {
            public override string SlotName { get; }

            public CurriculumPreparedSpellSlot(int level, string key, Trait school, string slotName)
                : base(level, key, school)
            {
                SlotName = slotName;
            }

#if V3
            public override string? DisallowsSpellBecause(Spell preparedSpell, CharacterSheet sheet, PreparedSpellSlots preparedSpellSlots)
            {
                // These spells may not have gone through the ActionOnEachSpell pipe, so patch them manually
                AddCurriculumTraits(preparedSpell.CombatActionSpell);
                return base.DisallowsSpellBecause(preparedSpell, sheet, preparedSpellSlots);
            }
#else
            public override bool AdmitsSpell(Spell preparedSpell, CharacterSheet sheet, PreparedSpellSlots preparedSpellSlots)
            {
                // These spells may not have gone through the ActionOnEachSpell pipe, so patch them manually
                AddCurriculumTraits(preparedSpell.CombatActionSpell);
                return base.AdmitsSpell(preparedSpell, sheet, preparedSpellSlots);
            }
#endif
        }

        public class CurriculumFeat : Feat
        {
            public CurriculumFeat(FeatName schoolFeat, Trait schoolTrait, string flavorText, SpellId focusSpell, SpellId[][] spellOptions)
                : base(schoolFeat, flavorText, "XX", new List<Trait>(), null)
            {
                Spell modernSpellTemplate = AllSpells.CreateModernSpellTemplate(focusSpell, Trait.Wizard);
                RulesText = "You gain an extra spell slot at each spell level for which you have wizard spell slots. You can only prepare spells from the " + Name + " in this slot.\n\nYou learn the " + AllSpells.CreateModernSpellTemplate(focusSpell, Trait.Wizard).ToSpellLink() + " focus school spell and you gain a focus pool of 1 focus point which recharges after every encounter.\n" + "\n{b}Curriculum{/b}";
                for (int spellRank = 0; spellRank < spellOptions.Length; spellRank++)
                {
                    string rankDescription = spellRank switch
                    {
                        0 => "cantrips",
                        1 => "1st",
                        2 => "2nd",
                        3 => "3rd",
                        _ => spellRank + "th",
                    };
                    IEnumerable<string> validSpellEntries = spellOptions[spellRank].Where((spellId) => spellId != SpellId.None).Select((spellId) => AllSpells.CreateModernSpellTemplate(spellId, Trait.Wizard).ToSpellLink());
                    if (validSpellEntries.Any())
                    {
                        RulesText += "\n" + rankDescription + ": " + string.Join(", ", validSpellEntries);
                    }
                };
                ShowRulesBlockForClassOfOrigin = Trait.Wizard;
                OnSheet = (sheet) =>
                {
                    if (schoolTrait != RemasterFeats.Trait.UnifiedMagicalTheory)
                    {
                        string schoolTraitName = TraitExtensions.TraitProperties[schoolTrait].HumanizedName;
                        sheet.WizardSchool = schoolTrait; // Create new traits for each curriculum
                        sheet.PreparedSpells.GetValueOrDefault(Trait.Wizard)?.Slots.Add(new CurriculumPreparedSpellSlot(0, "Wizard:SchoolSpell0:" + schoolTraitName, schoolTrait, schoolTraitName));
                        sheet.PreparedSpells.GetValueOrDefault(Trait.Wizard)?.Slots.Add(new CurriculumPreparedSpellSlot(1, "Wizard:SchoolSpell1:" + schoolTraitName, schoolTrait, schoolTraitName));
                        sheet.AddAtLevel(3, (laterValues) => laterValues.PreparedSpells.GetValueOrDefault(Trait.Wizard)?.Slots.Add(new CurriculumPreparedSpellSlot(2, "Wizard:SchoolSpell2:" + schoolTraitName, schoolTrait, schoolTraitName)));
                        sheet.AddAtLevel(5, (laterValues) => laterValues.PreparedSpells.GetValueOrDefault(Trait.Wizard)?.Slots.Add(new CurriculumPreparedSpellSlot(3, "Wizard:SchoolSpell3:" + schoolTraitName, schoolTrait, schoolTraitName)));
                        sheet.AddAtLevel(7, (laterValues) => laterValues.PreparedSpells.GetValueOrDefault(Trait.Wizard)?.Slots.Add(new CurriculumPreparedSpellSlot(4, "Wizard:SchoolSpell4:" + schoolTraitName, schoolTrait, schoolTraitName)));
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
            ModManager.RegisterActionOnEachSpell(AddCurriculumTraits);
        }


        private static void AddCurriculumTraits(CombatAction spellCombatAction)
        {
            List<Trait> schoolTraits = new List<Trait>();
            foreach (KeyValuePair<Trait, SpellId[][]> entry in curriculaSpellOptions)
            {
                if (entry.Value.Any((spellIds) => spellIds.Any((spellId) => spellId == spellCombatAction.SpellId)))
                {
                    schoolTraits.Add(entry.Key);
                }
            }
            if (schoolTraits.Any())
            {
                spellCombatAction.Traits.AddRange(schoolTraits);
            }
        }
    }
}
