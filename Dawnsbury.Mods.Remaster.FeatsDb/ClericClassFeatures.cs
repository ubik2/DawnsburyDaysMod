using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Modding;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.Roller;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;

namespace Dawnsbury.Mods.Remaster.FeatsDb
{
    public static class ClericClassFeatures
    {
        public static IEnumerable<Feat> LoadFonts()
        {

            yield return new Feat(FeatName.HealingFont, "Through your deity's blessing, you gain additional spells that channel the life force called vitality.", "You gain 4 additional spell slots each day at your highest level of cleric spell slots. You automatically prepare {i}heal{/i} spells in these slots.", new List<Trait> { Trait.DivineFont }, null)
                .WithOnSheet((CalculatedCharacterSheetValues sheet) =>
            {
                sheet.AtEndOfRecalculation = (Action<CalculatedCharacterSheetValues>)Delegate.Combine(sheet.AtEndOfRecalculation, (CalculatedCharacterSheetValues values) =>
                {
                    int num = 4;
                    for (int j = 0; j < num; j++)
                    {
                        values.PreparedSpells[Trait.Cleric].Slots.Add(new EnforcedPreparedSpellSlot(values.MaximumSpellLevel, "Healing Font", AllSpells.CreateModernSpellTemplate(SpellId.Heal, Trait.Cleric), "HealingFont:" + j));
                    }
                });
            }).WithIllustration(IllustrationName.Heal);
            yield return new Feat(FeatName.HarmfulFont, "Through your deity's blessing, you gain additional spells that channel the counterforce to life, the void.", "You gain 4 additional spell slots each day at your highest level of cleric spell slots. You automatically prepare {i}harm{/i} spells in these slots.", new List<Trait> { Trait.DivineFont }, null)
                .WithOnSheet((CalculatedCharacterSheetValues sheet) =>
            {
                sheet.AtEndOfRecalculation = (Action<CalculatedCharacterSheetValues>)Delegate.Combine(sheet.AtEndOfRecalculation, (CalculatedCharacterSheetValues values) =>
                {
                    int num = 4;
                    for (int i = 0; i < num; i++)
                    {
                        values.PreparedSpells[Trait.Cleric].Slots.Add(new EnforcedPreparedSpellSlot(values.MaximumSpellLevel, "Harmful Font", AllSpells.CreateModernSpellTemplate(SpellId.Harm, Trait.Cleric), "HarmfulFont:" + i));
                    }
                });
            }).WithIllustration(IllustrationName.Harm);
        }


        // Patch the heal spell to handle the Panic the Dead feat
        public static void PatchHeal()
        {
            ModManager.RegisterActionOnEachSpell((CombatAction originalSpell) => {
                if (originalSpell.SpellId != SpellId.Heal || originalSpell.Owner == null || !originalSpell.Owner.HasFeat(RemasterFeats.FeatName.PanicTheDead))
                {
                    return;
                }
                Delegates.EffectOnEachTarget extraEffect = async (CombatAction spell, Creature caster, Creature target, CheckResult checkResult) =>
                {
                    // This overlaps some with the base implementation, but the remaster version doesn't restrict by level and adds the frighteneed 1 on failure.
                    if (target.HasTrait(Trait.Undead))
                    {
                        if (checkResult != CheckResult.CriticalSuccess)
                        {
                            target.AddQEffect(QEffect.Frightened(1));
                        }
                        if (checkResult == CheckResult.CriticalFailure)
                        {
                            target.AddQEffect(QEffect.Fleeing(caster).WithExpirationAtStartOfSourcesTurn(caster, 1));
                        }
                    }
                    await Task.Delay(0);
                };
                originalSpell.EffectOnOneTarget = (Delegates.EffectOnEachTarget)Delegate.Combine(originalSpell.EffectOnOneTarget, extraEffect);
            });
        }

        public static SubmenuPossibility CreateSmiteSpellcastingMenu(Creature caster, Item weapon, string caption)
        {
            List<CombatAction> smiteSpells = new List<CombatAction>();
            if (caster.Spellcasting != null)
            {
                foreach (SpellcastingSource source in caster.Spellcasting.Sources)
                {
                    smiteSpells.AddRange(source.Spells.Where((spell) => spell.SpellId == SpellId.Harm || spell.SpellId == SpellId.Heal));
                }
            }
            IEnumerable<Possibility> possibilityList = smiteSpells.Select((CombatAction spell) => {
                CombatAction strikeAction = caster.CreateStrike(weapon).WithActionCost(2);
                strikeAction.Name = caption + ": " + spell.Name + " " + spell.SpellLevel;
                StrikeModifiers strikeModifiers = strikeAction.StrikeModifiers;
                strikeModifiers.OnEachTarget = (Func<Creature, Creature, CheckResult, Task>)Delegate.Combine(strikeModifiers.OnEachTarget, async (Creature caster, Creature target, CheckResult checkResult) =>
                {                 
                    caster.Spellcasting?.UseUpSpellcastingResources(spell);
                    if (target.DeathScheduledForNextStateCheck)
                    {
                        return;
                    }
                    bool canDamage = (spell.SpellId == SpellId.Heal && (target.HasTrait(Trait.Undead) || (caster.HasFeat(RemasterFeats.FeatName.DivineCastigation) && target.HasTrait(Trait.Fiend)))) ||
                        (spell.SpellId == SpellId.Harm && !target.HasTrait(Trait.Undead));
                    if (canDamage)
                    {
                        CheckResult savingThrowResult = checkResult switch
                        {
                            CheckResult.CriticalSuccess => CheckResult.CriticalFailure,
                            CheckResult.Success => CheckResult.Failure,
                            _ => CheckResult.CriticalSuccess
                        };
                        int dieValue = ((spell.SpellId == SpellId.Heal && caster.HasFeat(FeatName.HealingFont)) || (spell.SpellId == SpellId.Harm && caster.HasFeat(FeatName.HarmingHands))) ? 10 : 8;
                        bool isHeal = spell.SpellId == SpellId.Heal;
                        await CommonSpellEffects.DealBasicDamage(spell, caster, target, savingThrowResult, DiceFormula.FromText(spell.SpellLevel.ToString() + "d" + dieValue.ToString(), isHeal ? "Heal vitality damage" : "Harm void damage"), isHeal ? DamageKind.Positive : DamageKind.Negative);
                    }
                    else
                    {
                        caster.Battle.Log(spell.Name + " is ineffective.");
                    }
                });
                return new ActionPossibility(strikeAction);
            });
            SubmenuPossibility channelSmite = new SubmenuPossibility((Illustration)IllustrationName.TrueStrike, caption);
            PossibilitySection possibilitySection = new PossibilitySection(caption);
            possibilitySection.Possibilities.AddRange(possibilityList);
            channelSmite.Subsections.Add(possibilitySection);
            return channelSmite;
        }

        public static IEnumerable<Feat> LoadDeitySelectionFeats()
        {
            yield return new DeitySelectionFeat(FeatName.TheOracle, 
                "The Oracle is the most widely worshipped and revered deity on Our Point of Light, especially among the civilized folk. Well-aware that they are floating on a small insignificant plane through an endless void, the inhabitants of the plane put their faith in the Oracle, hoping that she would guide them and protect them from incomprehensible threats.\n\nAnd the Oracle, unlike most other deities, doesn't speak with her followers through signs or riddles. Instead, she maintains an open channel of verbal communication at her Grand Temple, though the temple's archclerics only rarely grant permission to ordinary folk to petition the Oracle.", "{b}• Edicts{/b} Care for each other, enjoy your life, protect civilization\n{b}• Anathema{/b} Travel into the Void, summon or ally yourself with demons",
                NineCornerAlignmentExtensions.All(), 
                new[] { FeatName.HealingFont, FeatName.HarmfulFont }, 
                new[] { FeatName.DomainHealing, FeatName.DomainTravel, FeatName.DomainLuck, FeatName.DomainSun }, 
                ItemName.Morningstar, 
                new[] { SpellId.Invisibility });

            yield return new DeitySelectionFeat(FeatName.TheBloomingFlower,
                "The only deity to have granted any spells during the Time of the Broken Sun, the Blooming Flower is perhaps responsible for the survival of civilization on Our Point of Light. She was instrumental in allowing the civilized folk to beat back the undead and summon food to carry us though the long night. Ever since then, she has become associated with sun and light and is respected for her role in the survival of Our Point of Light even by her enemies.\n\nIn spring, during the Day of Blossoms, she makes all plants on the plane blossom at the same time, covering every field, meadow and forest in a beautiful spectacle of bright colors. This causes winter to end and ushers in a one-week celebration during which all combat is not just forbidden, but impossible.", "{b}• Edicts{/b} Grow yourself, protect and enjoy nature, destroy the undead\n{b}• Anathema{/b} Break the stillness of forests, steal food, desecrate ancient sites",
                NineCornerAlignmentExtensions.All().Except(new[] { NineCornerAlignment.NeutralEvil, NineCornerAlignment.ChaoticEvil, NineCornerAlignment.LawfulEvil }).ToArray(),
                new[] { FeatName.HealingFont },
                new[] { FeatName.DomainSun, FeatName.DomainMoon, FeatName.DomainLuck, FeatName.DomainHealing },
                ItemName.Shortbow,
                new[] { RemasterFeats.GetUpdatedSpellId(SpellId.ColorSpray) });

            yield return new DeitySelectionFeat(FeatName.TheThunderingTsunami, 
                "A chaotic force of destruction, the Thundering Tsunami is best known for the Nights where Ocean Walks, unpredictable events that happen once in a generation when waves wash over our seaside settlements, and leave behind hundreds of destructive water elementals that wreak further havoc before they're killed or dry out and perish.\n\nDespite this destruction and despite being the most worshipped by evil water cults, the Thundering Tsunami is not evil herself and scholars believe that her destructive nights are a necessary component of the world's lifecycle, a release valve for pressure which would otherwise necessarily cause the plane itself to self-destruct.", "{b}• Edicts{/b} Build durable structures, walk at night, learn to swim\n{b}• Anathema{/b} Dive deep underwater, live on hills or inland, approach the edges of the world",
                NineCornerAlignmentExtensions.All().Except(new[] { NineCornerAlignment.LawfulGood, NineCornerAlignment.LawfulNeutral, NineCornerAlignment.LawfulEvil }).ToArray(),
                new[] { FeatName.HealingFont, FeatName.HarmfulFont },
                new[] { FeatName.DomainMoon, FeatName.DomainCold, FeatName.DomainTravel, FeatName.DomainDestruction },
                ItemName.Warhammer,
                new[] { RemasterFeats.GetUpdatedSpellId(SpellId.HideousLaughter) });

            yield return new DeitySelectionFeat(FeatName.TheUnquenchableInferno, 
                "The Unquenchable Inferno is the eternal fire that burns within the plane itself. The Inferno never answers any {i}commune{/i} rituals or other divinations, but each time a fire elemental dies, it releases a memory. This could be a single sentence or an hour-long recitation, and depending on the age and power of the elemental, it could be trivial minutiae of the elemental's life or important discussions on the nature of the cosmos.\n\nThe Keeper-Monks of the Ring of Fire consider fire to be the living memory of this plane and are funding expeditions to capture, kill and listen to elder fire elementals everywhere so that more fundamental truths may be revealed and shared.", "{b}• Edicts{/b} be prepared, battle your enemies, learn Ignan\n{b}• Anathema{/b} burn books, gain fire immunity",
                NineCornerAlignmentExtensions.All(),
                new[] { FeatName.HealingFont, FeatName.HarmfulFont },
                new[] { FeatName.DomainSun, FeatName.DomainFire, FeatName.DomainDestruction, FeatName.DomainDeath },
                ItemName.Earthbreaker,
                new[] { RemasterFeats.GetUpdatedSpellId(SpellId.BurningHands) });

            yield return new DeitySelectionFeat(FeatName.TheCeruleanSky,
                "Perhaps the most calm deity of them all, the Cerulean Sky manifests as the dome that shields us from the dangers of the Void during the day, but shows us the beauty of the Points of Light at night. It's the Cerulean Sky who connects leylines, draws water into clouds, and suffuses the land with both positive and negative energies, balancing the plane. She is the guardian of inanimate forces.\n\nBut perhaps because of her cold detachment and incomprehensible logic, over time she paradoxically became to be viewed more as the goddess of the night than of the daytime sky.", "{b}• Edicts{/b} Contemplate the sky, explore the world, fly\n{b}• Anathema{/b} Stay inside, create smoke, delve underground",
                NineCornerAlignmentExtensions.All(),
                new[] { FeatName.HealingFont, FeatName.HarmfulFont },
                new[] { FeatName.DomainMoon, FeatName.DomainCold, FeatName.DomainTravel, FeatName.DomainUndeath },
                ItemName.Falchion,
                new[] { RemasterFeats.GetUpdatedSpellId(SpellId.ShockingGrasp) });
        }

        /// <summary>
        ///  After adding our deity selection feats, we need to update the ClassSelectionFeat feat (since it has its own copy of this list)
        /// </summary>
        public static void PatchClassDeities()
        {
            ClassSelectionFeat classFeat = (ClassSelectionFeat)AllFeats.All.First((feat) => feat.FeatName == FeatName.Cleric);
            classFeat.Subfeats = AllFeats.All.Where((feat) => feat.HasTrait(Trait.ClericDeity)).ToList();
        }
    }
}
