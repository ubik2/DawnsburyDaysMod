using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.AbilityScores;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Creatures.Parts;
using Dawnsbury.Core.Animations;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.Battlecry
{
    internal class Commander
    {
        // Armored Regiment Training - not useful in game
        // Combat Assessment - not useful in game
        // Combat Medic - TODO
        // Commander's Steed - no horses (large), but maybe implement?
        // Deceptive Tactics - TODO
        // Plant Banner - TODO

        // Adaptive Stratagem - Maybe? Probably skip
        // Defensive Swap - too many prompts?
        // Guiding Shot - TODO
        // Set-up Strike - TODO
        // Tactical Expansion - probably skip
        // Rapid Assessment - not useful in game

        // Banner Twirl - TODO
        // Observational Analysis - not useful in game
        // Shielded Recovery - TODO
        // Wave the Flag - TODO

        public static IEnumerable<Feat> LoadFeats()
        {
            yield return new TrueFeat(BattlecryMod.FeatName.CombatMedic, 1, "You’re trained in battlefield triage and wound treatment.",
                "You are trained in Medicine and can use your Intelligence modifier in place of your Wisdom modifier for Medicine checks. You gain the Battle Medicine feat.",
                [BattlecryMod.Trait.Commander]).WithOnSheet((CalculatedCharacterSheetValues sheet) =>
                {
                    sheet.GrantFeat(FeatName.Medicine);
                    sheet.GrantFeat(FeatName.BattleMedicine);
                }).WithOnCreature((creature) =>
                {
                    creature.AddQEffect(new QEffect() {
                        BonusToSkills = ((skill) => (skill == Skill.Medicine && creature.Abilities.Intelligence > creature.Abilities.Wisdom) ? new Bonus(creature.Abilities.Intelligence - creature.Abilities.Wisdom, BonusType.Untyped, "Combat Medic", true) : null)
                    });
                });

            yield return new TrueFeat(BattlecryMod.FeatName.CommandersSteed, 1, "You gain the service of a young animal companion as a mount.",
                "You can affix your banner to your mount's saddle or barding, determining the effects of your commander's banner and other abilities that use your banner from your mount's space, even if you are not currently riding your mount. Typically, the steed is an animal companion with the mount ability (such as a horse).",
                [BattlecryMod.Trait.Commander],
                [
                    CommanderAnimalCompanion.CreateAnimalCompanionFeat(FeatName.AnimalCompanionBat, "Your companion is a particularly large bat."),
                    CommanderAnimalCompanion.CreateAnimalCompanionFeat(FeatName.AnimalCompanionBear, "Your companion is a black, grizzly, polar, or other type of bear."),
                    CommanderAnimalCompanion.CreateAnimalCompanionFeat(FeatName.AnimalCompanionBird, "Your companion is a bird of prey, such as an eagle, hawk, or owl."),
                    CommanderAnimalCompanion.CreateAnimalCompanionFeat(FeatName.AnimalCompanionPangolin, "Your companion is an unusually large hard-scaled beast, such as a pangolin."),
                    CommanderAnimalCompanion.CreateAnimalCompanionFeat(FeatName.AnimalCompanionCapybara, "Your companion is a capybara, a giant rodent common in the forests of the Blooming South.")
                ]).WithPrerequisite((values) => values.Sheet.Class?.ClassTrait == BattlecryMod.Trait.Commander, "You must be a commander.");

            yield return new TrueFeat(BattlecryMod.FeatName.DeceptiveTactics, 1, "Your training has taught you that the art of war is the art of deception.",
                "You can use your Warfare Lore modifier in place of your Deception modifier for Deception checks to Create a Diversion or Feint, and can use your proficiency rank in Warfare Lore instead of your proficiency rank in Deception to meet the prerequisites of feats that modify the Create a Diversion or Feint actions (such as Lengthy Diversion).",
                [BattlecryMod.Trait.Commander])
                .WithOnCreature((sheet, creature) =>
                {
                    int baseMod = sheet.FinalAbilityScores.TotalModifier(Skills.GetSkillAbility(Skill.Deception)) + sheet.GetProficiency(Trait.Deception).ToNumber(creature.Level);
                    int warfareLoreProficiency = creature.Level switch { < 3 => 2, < 7 => 4, < 15 => 6, _ => 8 };
                    int newMod = sheet.FinalAbilityScores.TotalModifier(Ability.Intelligence) + warfareLoreProficiency;
                    if (newMod > baseMod)
                    {
                        creature.AddQEffect(new QEffect()
                        {
                            BonusToSkills = ((skill) => (skill == Skill.Deception) ? new Bonus(newMod - baseMod, BonusType.Untyped, "Deceptive Tactics", true) : null)
                        });
                    }
                });


        }

        public static IEnumerable<Feat> LoadAll()
        {
            foreach (Feat tactic in LoadTactics())
            {
                yield return tactic;
            }

            foreach (Feat feat in LoadFeats())
            {
                yield return feat;
            }

            yield return CommandersBanner();

            yield return new ClassSelectionFeat(BattlecryMod.FeatName.Commander,
                "You approach battle with the knowledge that tactics and strategy are every bit as crucial as brute strength or numbers. You may have trained in classical theories of warfare and strategy at a military school or you might have refined your techniques through hard-won experience as part of an army or mercenary company. Regardless of how you came by your knowledge, you have a gift for signaling your allies from across the battlefield and shouting commands to rout even the most desperate conflicts, allowing your squad to exceed their limits and claim victory.",
                BattlecryMod.Trait.Commander, new EnforcedAbilityBoost(Ability.Intelligence), 8,
                [Trait.Fortitude, Trait.Armor, Trait.UnarmoredDefense, Trait.Society, Trait.Simple, Trait.Martial, Trait.Unarmed], // Trait.WarfareLore
                [Trait.Reflex, Trait.Will, Trait.Perception],
                2,
                "{b}1. Commander's Banner{/b} A commander needs a battle standard so their allies can locate them on the field. You start play with a custom banner that you can use to signal allies when using tactics or to deploy specific abilities.\n\n" +
                "{b}2. Tactics{/b} By studying and practicing the strategic arts of war, you can guide your allies to victory.\n\n" +
                "{b}3. Drilled Reactions{/b} Your time spent training with your allies allows them to respond quickly and instinctively to your commands.\n\n" +
                "{b}4. Shield Block {icon:Reaction}.{/b} You gain the Shield Block general feat.",
                null)
                .WithOnSheet((CalculatedCharacterSheetValues sheet) =>
                {
                    sheet.GrantFeat(BattlecryMod.FeatName.CommandersBanner);
                    sheet.GrantFeat(FeatName.ShieldBlock);
                    sheet.AddSelectionOption(new SingleFeatSelectionOption("CommanderFeat1", "Commander feat", 1, (feat) => feat.HasTrait(BattlecryMod.Trait.Commander)));
                    sheet.AddSelectionOption(new SingleFeatSelectionOption("CommanderTactics1", "Commander tactics", 1, (feat) => feat.HasTrait(BattlecryMod.Trait.Tactic)));
                    sheet.AddSelectionOption(new SingleFeatSelectionOption("CommanderTactics2", "Commander tactics", 1, (feat) => feat.HasTrait(BattlecryMod.Trait.Tactic)));
                });
        }

        // Defensive Retreat - not too strict on movement
        // Form Up - TODO
        // Mountaineering Training - not useful in game
        // Naval Training - not useful in game
        // Passage of Lines - maybe? might be complicated
        // Coordinating Maneuvers - TODO
        // Double Team - TODO
        // End It - maybe
        // Pincer Attack
        // Reload - TODO
        // Shields Up! - TODO
        // Strike Hard! - TODO
        public static IEnumerable<Feat> LoadTactics()
        {
            yield return new Feat(BattlecryMod.FeatName.FormUp, "You signal your team to move into position together.",
                "Signal all squadmates affected by your commander's banner; each can immediately Stride as a reaction, though each must end their movement inside your banner’s aura.",
                [BattlecryMod.Trait.Tactic, BattlecryMod.Trait.Commander], null)
            .WithPermanentQEffect("You signal an aggressive formation designed to exploit enemies' vulnerabilities.", (qEffect) => qEffect.ProvideMainAction = (qfTactic) =>
            {
                return new ActionPossibility(FormUp(qEffect.Owner)).WithPossibilityGroup(nameof(Commander));
            });

            yield return new Feat(BattlecryMod.FeatName.PincerAttack, "You signal an aggressive formation designed to exploit enemies' vulnerabilities.",
                "Signal all squadmates affected by your commander's banner; each can Step as a free action. If any of your allies end this movement adjacent to an opponent, that opponent is off-guard to melee attacks from you and all other squadmates who responded to Pincer Attack until the start of your next turn.",
                [BattlecryMod.Trait.Tactic, BattlecryMod.Trait.Commander], null)
            .WithPermanentQEffect("You signal an aggressive formation designed to exploit enemies' vulnerabilities.", (qEffect) => qEffect.ProvideMainAction = (qfTactic) =>
            {
                return new ActionPossibility(PincerAttack(qEffect.Owner)).WithPossibilityGroup(nameof(Commander));
            });

            yield return new Feat(BattlecryMod.FeatName.StrikeHard, "You command an ally to attack.",
                "Choose a squadmate who can see or hear your signal. That ally immediately attempts a Strike as a reaction.",
                [BattlecryMod.Trait.Tactic, BattlecryMod.Trait.Commander], null)
            .WithPermanentQEffect("You command an ally to attack.", (qEffect) => qEffect.ProvideMainAction = (qfTactic) =>
            {
                return new ActionPossibility(StrikeHard(qEffect.Owner)).WithPossibilityGroup(nameof(Commander));
            });

            yield break;
        }

        private static CombatAction FormUp(Creature owner)
        {
            // We'll automatically use Drilled Reactions if available
            CombatAction tactic = new CombatAction(owner, IllustrationName.GenericCombatManeuver, "Form Up!", [BattlecryMod.Trait.Banner, BattlecryMod.Trait.Tactic, BattlecryMod.Trait.Commander],
                "You signal your team to move into position together." +
                "Signal all squadmates affected by your commander's banner; each can immediately Stride as a reaction, though each must end their movement inside your banner’s aura.",
                Target.Emanation(6).WithIncludeOnlyIf((area, t) => t.FriendOfAndNotSelf(owner)))
                .WithActionCost(1)
                .WithSoundEffect(SfxName.BeastRoar)
                .WithEffectOnEachTarget(async delegate (CombatAction action, Creature caster, Creature target, CheckResult checkResult)
                {
                    if (new TacticResponseRequirement().Satisfied(caster, target) != Usability.Usable)
                    {
                        return;
                    }
                    // Option to take a free stride
                    bool moved = await target.StrideAsync(target.Name + ": Pincer Attack Step", allowPass: true);
                    if (moved)
                    {
                        bool useDrilledReactions = !caster.QEffects.Any((qEffect) => qEffect.Name == "Drilled Reactions Expended");
                        if (useDrilledReactions)
                        {
                            caster.AddQEffect(new QEffect("Drilled Reactions Expended", "Drilled Reactions has already been used.").WithExpirationAtStartOfSourcesTurn(caster, 1));
                        }
                        target.AddQEffect(new QEffect("Responded to Tactic", "You have responded to a Commander Tactic this round.").WithExpirationAtStartOfSourcesTurn(caster, 1));
                        if (!useDrilledReactions)
                        {
                            target.Actions.UseUpReaction();
                        }
                    }
                });
            return tactic;
        }

        private static CombatAction PincerAttack(Creature owner)
        {
            List<Creature> includedAllies = new List<Creature>();
            CombatAction pincerAttack = new CombatAction(owner, IllustrationName.GenericCombatManeuver, "Pincer Attack", [BattlecryMod.Trait.Commander, BattlecryMod.Trait.Tactic],
                "You signal an aggressive formation designed to exploit enemies' vulnerabilities." +
                "Signal all squadmates affected by your commander's banner; each can Step as a free action. If any of your allies end this movement adjacent to an opponent, that opponent is off-guard to melee attacks from you and all other squadmates who responded to Pincer Attack until the start of your next turn.",
                Target.Emanation(6).WithIncludeOnlyIf((area, t) => t.FriendOfAndNotSelf(owner)))
                .WithActionCost(1)
                .WithSoundEffect(SfxName.BeastRoar)
                .WithEffectOnEachTarget(async delegate (CombatAction action, Creature caster, Creature target, CheckResult checkResult)
                {
                    if (new TacticResponseRequirement().Satisfied(caster, target) != Usability.Usable)
                    {
                        return;
                    }
                    // Option to take a free step
#if V3
                    bool stepped = await target.StepAsync(target.Name + ": Pincer Attack Step", allowPass: true);
#else
                    bool stepped = await target.StrideAsync(target.Name + ": Pincer Attack Step", allowStep: true, maximumFiveFeet: true, allowPass: true);
#endif
                    if (stepped)
                    {
                        target.AddQEffect(new QEffect("Responded to Tactic", "You have responded to a Commander Tactic this round.").WithExpirationAtStartOfSourcesTurn(caster, 1));
                        includedAllies.Add(target);
                        foreach (Creature creature in target.Occupies.Neighbours.Creatures)
                        {
                            if (creature.EnemyOf(target) && !creature.QEffects.Any((qEffect) => qEffect.Name == "Pincer Attack Vulnerability"))
                            {
                                creature.AddQEffect(new QEffect("Pincer Attack Vulnerability", "Off-guard to melee attacks from participating attackers.")
                                {
                                    Illustration = IllustrationName.Flatfooted,
                                    Tag = includedAllies,
                                    IsFlatFootedTo = (qEffect, attacker, combatAction) =>
                                    {
                                        List<Creature> includedAttackers = (List<Creature>)qEffect.Tag!;
                                        if (combatAction != null && combatAction.HasTrait(Trait.Attack) && combatAction.HasTrait(Trait.Melee) &&
                                            attacker != null && includedAttackers != null && includedAttackers.Contains(attacker))
                                        {
                                            return "Pincer Attack";
                                        }
                                        return null;
                                    }
                                }.WithExpirationAtStartOfSourcesTurn(caster, 1));
                            }
                        }
                    }
                });
            return pincerAttack;
        }

        private static CombatAction StrikeHard(Creature owner)
        {
            // We'll automatically use Drilled Reactions if available
            CombatAction strikeHard = new CombatAction(owner, IllustrationName.GenericCombatManeuver, "Strike Hard!", [BattlecryMod.Trait.Banner, BattlecryMod.Trait.Tactic, BattlecryMod.Trait.Commander],
                "You signal an aggressive formation designed to exploit enemies' vulnerabilities." +
                "Choose a squadmate who can see or hear your signal. That ally immediately attempts a Strike as a reaction.",
                new CreatureTarget(RangeKind.Ranged, 
                    [new MaximumRangeCreatureTargetingRequirement(6), new FriendCreatureTargetingRequirement(), new UnblockedLineOfEffectCreatureTargetingRequirement(), new TacticResponseRequirement(), new ReactionRequirement(), new PrimaryWeaponRequirement()], 
                    (Target self, Creature you, Creature empty) => -2.14748365E+09f))
                .WithActionCost(2)
                .WithSoundEffect(SfxName.BeastRoar)
                .WithEffectOnEachTarget(async delegate (CombatAction action, Creature caster, Creature target, CheckResult checkResult)
                {
                    bool useDrilledReactions = !caster.QEffects.Any((qEffect) => qEffect.Name == "Drilled Reactions Expended");
                    if (useDrilledReactions)
                    {
                        caster.AddQEffect(new QEffect("Drilled Reactions Expended", "Drilled Reactions has already been used.").WithExpirationAtStartOfSourcesTurn(caster, 1));
                    }
                    target.AddQEffect(new QEffect("Responded to Tactic", "You have responded to a Commander Tactic this round.").WithExpirationAtStartOfSourcesTurn(caster, 1));
                    // Free strike with no MAP
                    CombatAction strike = target.CreateStrike(target.PrimaryWeapon!).WithActionCost(0);
                    int map = target.Actions.AttackedThisManyTimesThisTurn;
                    target.Actions.AttackedThisManyTimesThisTurn = 0;
                    await target.Battle.GameLoop.FullCast(strike);
                    target.Actions.AttackedThisManyTimesThisTurn = map;
                    // We either used the single free reaction from Drilled Reactions, or the target used their reaction.
                    if (!useDrilledReactions)
                    {
                        target.Actions.UseUpReaction();
                    }
                });
            return strikeHard;
        }

        private static Feat CommandersBanner()
        {
            int radius = 6;
            return new Feat(BattlecryMod.FeatName.CommandersBanner, "A commander needs a battle standard so their allies can locate them on the field.",
                "As long as your banner is visible, you and all allies in a 30-foot emanation gain a +1 status bonus to Will saves and DCs against fear effects.",
                [BattlecryMod.Trait.Commander], null)
                .WithOnCreature((sheet, caster) =>
                {
                    AuraAnimation auraAnimation = caster.AnimationData.AddAuraAnimation(IllustrationName.BlessCircle, radius);
                    auraAnimation.Color = Color.Coral;
                    caster.AddQEffect(new QEffect("Commander's Banner", "You and all allies in a 30-foot emanation gain a +1 status bonus to Will saves and DCs against fear effects.")
                    {
                        StateCheck = (qfBanner) =>
                        {
                            foreach (Creature friend in qfBanner.Owner.Battle.AllCreatures.Where((Creature cr) => cr.DistanceTo(qfBanner.Owner) <= radius && cr.FriendOf(qfBanner.Owner) && !cr.HasTrait(Trait.Mindless) && !cr.HasTrait(Trait.Object)))
                            {
                                friend.AddQEffect(new QEffect("Commander's Banner", "You gain a +1 status bonus to Will saves and DCs against fear effects.", ExpirationCondition.Ephemeral, qfBanner.Owner)
                                {
                                    CountsAsABuff = true,
                                    BonusToDefenses = (qEffect, combatAction, defense) =>
                                    {
                                        if (combatAction != null && combatAction.HasTrait(Trait.Fear) && defense == Defense.Will)
                                        {
                                            return new Bonus(1, BonusType.Status, "Commander's Banner", true);
                                        }
                                        return null;
                                    }
                                });
                            }
                        },
                        ExpiresAt = ExpirationCondition.Never,
                        WhenExpires = (qfBanner) => auraAnimation.MoveTo(0.0f)
                    });
                });
        }

        #region Extra Targeting Requirement Classes
        public class ReactionRequirement : CreatureTargetingRequirement
        {
            public override Usability Satisfied(Creature source, Creature target)
            {
                if (source.QEffects.Any((qEffect) => qEffect.Name == "Drilled Reactions Expended") && target.Actions.IsReactionUsedUp)
                {
                    return Usability.NotUsableOnThisCreature("You have used your Driled Reactions already, and your target doesn't have a reaction available.");
                }
                return Usability.Usable;
            }
        }

        public class TacticResponseRequirement : CreatureTargetingRequirement {
            public override Usability Satisfied(Creature source, Creature target)
            {
                if (target.QEffects.Any((qEffect) => qEffect.Name == "Responded to Tactic"))
                {
                    return Usability.NotUsableOnThisCreature(target.Name + " has already responded to a tactic this round.");
                }
                return Usability.Usable;
            }
        }

        public class PrimaryWeaponRequirement : CreatureTargetingRequirement
        {
            public override Usability Satisfied(Creature source, Creature target)
            {
                if (target.PrimaryWeapon == null)
                {
                    return Usability.NotUsableOnThisCreature(target.Name + " does not have a valid weapon.");
                }
                return Usability.Usable;
            }
        }
        #endregion


    }
}
