using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Creatures.Parts;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Damage;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Roller;
using Dawnsbury.Display;
using Dawnsbury.Display.Illustrations;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.Battlecry
{
    internal class CommanderAnimalCompanion
    {
        public static Feat CreateAnimalCompanionFeat(FeatName featName, string flavorText)
        {
            Creature creature = CreateAnimalCompanion(featName, 1);
            creature.RegeneratePossibilities();
            foreach (QEffect qeffect in creature.QEffects.ToList<QEffect>())
            {
                Action<QEffect>? stateCheck = qeffect.StateCheck;
                if (stateCheck != null)
                    stateCheck(qeffect);
            }
            creature.RecalculateLandSpeed();
            return new Feat(featName, flavorText, "Your animal companion has the following characteristics at level 1:\n\n" + RulesBlock.CreateCreatureDescription(creature), new List<Trait>(), null).WithIllustration(creature.Illustration).WithOnCreature(((sheet, ranger) => ranger.AddQEffect(new QEffect()
            {
                StartOfCombat = async (qfRangerTechnical) =>
                {
                    if (ranger.PersistentUsedUpResources.AnimalCompanionIsDead)
                    {
                        ranger.Occupies.Overhead("no companion", Color.Green, ranger?.ToString() + "'s animal companion is dead. A new animal companion will find you after your next long rest or downtime.");
                    }
                    else
                    {
                        Creature animalCompanion = CreateAnimalCompanion(featName, ranger.Level);
                        animalCompanion.MainName = qfRangerTechnical.Owner.Name + "'s " + animalCompanion.MainName;
                        animalCompanion.AddQEffect(new QEffect()
                        {
                            Id = QEffectId.RangersCompanion,
                            Source = ranger,
                            WhenMonsterDies = (Action<QEffect>)(qfCompanion => ranger.PersistentUsedUpResources.AnimalCompanionIsDead = true)
                        });
                        Action<Creature, Creature>? benefitsToCompanion = sheet.RangerBenefitsToCompanion;
                        if (benefitsToCompanion != null)
                            benefitsToCompanion(animalCompanion, ranger);
                        ranger.Battle.SpawnCreature(animalCompanion, ranger.OwningFaction, ranger.Occupies);
                    }
                },
                EndOfYourTurn = async (qfRanger, self) =>
                {
                    if (qfRanger.UsedThisTurn)
                        return;
                    Creature? animalCompanion = Ranger.GetAnimalCompanion(qfRanger.Owner);
                    if (animalCompanion == null)
                        return;
                    await animalCompanion.Battle.GameLoop.EndOfTurn(animalCompanion);
                },
                ProvideMainAction = (qfRanger) =>
                {
                    Creature? animalCompanion = Ranger.GetAnimalCompanion(qfRanger.Owner);
                    if (animalCompanion == null || !animalCompanion.Actions.CanTakeActions())
                        return null;
                    return (ActionPossibility)new CombatAction(qfRanger.Owner, creature.Illustration, "Command your Animal Companion", [Trait.Auditory], "Take 2 actions as your animal companion.\n\nYou can only command your animal companion once per turn.", 
                        Target.Self().WithAdditionalRestriction((self) => qfRanger.UsedThisTurn ? "You already commanded your animal companion this turn." : null))
                    {
                        ShortDescription = "Take 2 actions as your animal companion."
                    }.WithEffectOnSelf(async (self) =>
                    {
                        qfRanger.UsedThisTurn = true;
                        await CommonSpellEffects.YourMinionActs(animalCompanion);
                    });
                }
            })));
        }

        private static Creature? GetRanger(Creature companion)
        {
            return companion.QEffects.FirstOrDefault((qf) => qf.Id == QEffectId.RangersCompanion)?.Source;
        }

        private static Creature CreateAnimalCompanionBase(IllustrationName illustration, string name, int level, Ability increase1, Ability increase2, int speed, int ancestryHp, Skill trainedSkill)
        {
            int strength = 2 + (increase1 == Ability.Strength || increase2 == Ability.Strength ? 1 : 0);
            int dexterity = 2 + (increase1 == Ability.Dexterity || increase2 == Ability.Dexterity ? 1 : 0);
            int constitution = 1 + (increase1 == Ability.Constitution || increase2 == Ability.Constitution ? 1 : 0);
            int intelligence = (increase1 == Ability.Intelligence || increase2 == Ability.Intelligence ? 1 : 0) - 4;
            int wisdom = 1 + (increase1 == Ability.Wisdom || increase2 == Ability.Wisdom ? 1 : 0);
            int charisma = increase1 == Ability.Charisma || increase2 == Ability.Charisma ? 1 : 0;
            int num = 2 + level;
            Abilities abilities1 = new Abilities(strength, dexterity, constitution, intelligence, wisdom, charisma);
            Skills skills1 = new Skills(dexterity + num, athletics: strength + num);
            skills1.Set(trainedSkill, abilities1.Get(Skills.GetSkillAbility(trainedSkill)) + num);
            Illustration illustration1 = (Illustration)illustration;
            string name1 = name;
            List<Trait> traits = [Trait.Animal, Trait.Minion, Trait.AnimalCompanion];
            int level1 = level;
            int perception = wisdom + num;
            int speed1 = speed;
            Defenses defenses = new Defenses(10 + dexterity + num, constitution + num, dexterity + num, wisdom + num);
            int hp = ancestryHp + (6 + constitution) * level;
            Abilities abilities2 = abilities1;
            Skills skills2 = skills1;
            return new Creature(illustration1, name1, traits, level1, perception, speed1, defenses, hp, abilities2, skills2).WithProficiency(Trait.Unarmed, Proficiency.Trained).WithEntersInitiativeOrder(false).WithProficiency(Trait.UnarmoredDefense, Proficiency.Trained).AddQEffect(new QEffect()
            {
                StateCheck = (sc) =>
                {
                    if (sc.Owner.HasEffect(QEffectId.Dying) || !sc.Owner.Battle.InitiativeOrder.Contains(sc.Owner))
                        return;
                    Creature owner = sc.Owner;
                    int index = (owner.Battle.InitiativeOrder.IndexOf(owner) + 1) % owner.Battle.InitiativeOrder.Count;
                    Creature creature = owner.Battle.InitiativeOrder[index];
                    owner.Actions.HasDelayedYieldingTo = creature;
                    if (owner.Battle.CreatureControllingInitiative == owner)
                        owner.Battle.CreatureControllingInitiative = creature;
                    owner.Battle.InitiativeOrder.Remove(sc.Owner);
                }
            });
        }

        internal static Creature CreateAnimalCompanion(FeatName featName, int level)
        {
            Creature creature1;
            switch (featName)
            {
                case FeatName.AnimalCompanionBat:
                    creature1 = CreateAnimalCompanionBase(IllustrationName.Bat256, "Bat", level, Ability.Dexterity, Ability.Constitution, 6, 6, Skill.Stealth).AddQEffect(QEffect.Flying())
                        .WithUnarmedStrike(new Item(IllustrationName.Jaws, "jaws", [Trait.Unarmed, Trait.Finesse]).WithWeaponProperties(new WeaponProperties("1d6", DamageKind.Piercing)))
                        .WithAdditionalUnarmedStrike(CommonItems.CreateNaturalWeapon(IllustrationName.Wing, "wing", "1d4", DamageKind.Slashing, Trait.Agile, Trait.Finesse));
                    creature1 = AddSupportAction(creature1, "Your bat flaps around your foes' arms and faces, getting in the way of their attacks.", 
                        "Until the start of your next turn, creatures in your bat's reach that you damage with Strikes take a –1 circumstance penalty to their attack rolls.", (qf) => qf.StateCheck = (_) =>
                    {
                        Creature companion = qf.Owner;
                        Creature? ranger = GetRanger(companion);
                        ranger?.AddQEffect(new QEffect(ExpirationCondition.Ephemeral)
                        {
                            AfterYouDealDamage = async (creature, action, defender) =>
                            {
                                if (!action.HasTrait(Trait.Strike) || !defender.IsAdjacentTo(companion))
                                    return;
                                defender.AddQEffect(new QEffect("Bat flaps", "You get -1 to attack rolls.", ExpirationCondition.ExpiresAtStartOfSourcesTurn, ranger, (Illustration)IllustrationName.Wing)
                                {
                                    BonusToAttackRolls = (_, attack, _) => !attack.HasTrait(Trait.Attack) ? null : new Bonus(-1, BonusType.Circumstance, "Bat flaps")
                                });
                            }
                        });
                    });
                    break;
                case FeatName.AnimalCompanionBear:
                    creature1 = CreateAnimalCompanionBase(IllustrationName.Bear256, "Bear", level, Ability.Strength, Ability.Constitution, 7, 8, Skill.Intimidation)
                        .WithUnarmedStrike(CommonItems.CreateNaturalWeapon(IllustrationName.Jaws, "jaws", "1d8", DamageKind.Piercing))
                        .WithAdditionalUnarmedStrike(CommonItems.CreateNaturalWeapon(IllustrationName.DragonClaws, "claw", "1d6", DamageKind.Slashing, Trait.Agile));
                    creature1 = AddSupportAction(creature1, "Your bear mauls your enemies when you create an opening.",
                        "Until the start of your next turn, each time you hit a creature in the bear's reach with a Strike, the creature takes 1d8 slashing damage from the bear.", (qf) => qf.StateCheck = (_) =>
                        {
                            Creature companion = qf.Owner;
                            Creature? ranger = GetRanger(companion);
                            ranger?.AddQEffect(new QEffect(ExpirationCondition.Ephemeral)
                            {
                                AfterYouDealDamage = async (creature, action, defender) =>
                                {
                                    if (!action.HasTrait(Trait.Strike) || !defender.IsAdjacentTo(companion))
                                        return;
                                    await companion.FictitiousSingleTileMove(defender.Occupies);
                                    await CommonSpellEffects.DealDirectDamage(new DamageEvent(null, defender, CheckResult.Failure, 
                                        [new KindedDamage(DiceFormula.FromText("1d8", "Bear maul"), DamageKind.Slashing)]));
                                    await companion.FictitiousSingleTileMove(companion.Occupies);
                                }
                            });
                        });
                    break;
                case FeatName.AnimalCompanionBird:
                    creature1 = CreateAnimalCompanionBase(IllustrationName.Bird256, "Bird", level, Ability.Dexterity, Ability.Wisdom, 12, 4, Skill.Stealth).AddQEffect(QEffect.Flying())
                        .WithUnarmedStrike(CommonItems.CreateNaturalWeapon(IllustrationName.Jaws, "jaws", "1d6", DamageKind.Piercing, Trait.Finesse))
                        .WithAdditionalUnarmedStrike(CommonItems.CreateNaturalWeapon(IllustrationName.DragonClaws, "talon", "1d4", DamageKind.Slashing, Trait.Agile, Trait.Finesse));
                    creature1 = AddSupportAction(creature1, "The bird pecks at your foes' eyes when you create an opening.",
                        "Until the start of your next turn, your Strikes that damage a creature that your bird threatens also deal 1d4 persistent bleed damage, and the target is dazzled until it removes the bleed damage.", (qf) => qf.StateCheck = (_) =>
                        {
                            Creature companion = qf.Owner;
                            Creature? ranger = GetRanger(companion);
                            ranger?.AddQEffect(new QEffect(ExpirationCondition.Ephemeral)
                            {
                                AfterYouDealDamage = async (creature, action, defender) =>
                                {
                                    if (!action.HasTrait(Trait.Strike) || !defender.IsAdjacentTo(companion))
                                        return;
                                    await companion.FictitiousSingleTileMove(defender.Occupies);
                                    QEffect qEffect = QEffect.PersistentDamage("1d4", DamageKind.Bleed);
                                    QEffect dazzled = QEffect.Dazzled().WithExpirationNever();
                                    qEffect.WhenExpires += (Action<QEffect>)(qfPersistentDamage => dazzled.ExpiresAt = ExpirationCondition.Immediately);
                                    defender.AddQEffect(qEffect);
                                    defender.AddQEffect(dazzled);
                                    await companion.FictitiousSingleTileMove(companion.Occupies);
                                }
                            });
                        });
                    break;
                case FeatName.AnimalCompanionPangolin:
                    creature1 = CreateAnimalCompanionBase(IllustrationName.Pangolin256, "Pangolin", level, Ability.Strength, Ability.Constitution, 5, 8, Skill.Survival)
                        .WithUnarmedStrike(CommonItems.CreateNaturalWeapon(IllustrationName.Slam, "body", "1d8", DamageKind.Bludgeoning))
                        .WithAdditionalUnarmedStrike(CommonItems.CreateNaturalWeapon(IllustrationName.DragonClaws, "claw", "1d6", DamageKind.Slashing, Trait.Agile));
                    creature1 = AddSupportAction(creature1, "Your pangolin tears at your enemies with its serrated plates.",
                        "Until the start of your next turn, your Strikes that damage a creature in your pangolin's reach also deal 1d6 persistent bleed damage.", (qf) => qf.StateCheck = (_) =>
                        {
                            Creature companion = qf.Owner;
                            Creature? ranger = GetRanger(companion);
                            ranger?.AddQEffect(new QEffect(ExpirationCondition.Ephemeral)
                            {
                                AfterYouDealDamage = async (creature, action, defender) =>
                                {
                                    if (!action.HasTrait(Trait.Strike) || !defender.IsAdjacentTo(companion))
                                        return;
                                    await companion.FictitiousSingleTileMove(defender.Occupies);
                                    defender.AddQEffect(QEffect.PersistentDamage("1d6", DamageKind.Bleed));
                                    await companion.FictitiousSingleTileMove(companion.Occupies);
                                }
                            });
                        });
                    break;
                case FeatName.AnimalCompanionCapybara:
                    creature1 = CreateAnimalCompanionBase(IllustrationName.Capybara256, "Capybara", level, Ability.Constitution, Ability.Wisdom, 6, 6, Skill.Survival)
                        .WithUnarmedStrike(CommonItems.CreateNaturalWeapon(IllustrationName.Capybara256, "head", "1d6", DamageKind.Bludgeoning, Trait.Agile));
                    creature1 = AddSupportAction(creature1, "Your capybara assists you in battle.",
                        "You gain a +1 circumstance bonus on your next attack roll to Strike a foe within your capybara's reach. The bonus lasts until the first time you use it or until the beginning of your next turn, whichever comes first.", (qf) => qf.StateCheck = (_) =>
                        {
                            Creature companion = qf.Owner;
                            Creature? ranger = GetRanger(companion);
                            ranger?.AddQEffect(new QEffect(ExpirationCondition.Ephemeral)
                            {
                                BonusToAttackRolls = (qfSupport, action, defender) => defender != null && action.HasTrait(Trait.Strike) && defender.IsAdjacentTo(companion) ? new Bonus(1, BonusType.Circumstance, "Capybara's support") : null,
                                AfterYouTakeAction = async (effect, action) =>
                                {
                                    if (!action.HasTrait(Trait.Strike))
                                        return;
                                    Creature? chosenCreature = action.ChosenTargets.ChosenCreature;
                                    if ((chosenCreature != null ? (chosenCreature.IsAdjacentTo(companion) ? 1 : 0) : 0) == 0)
                                        return;
                                    qf.ExpiresAt = ExpirationCondition.Immediately;
                                }
                            });
                        });
                    break;
                default:
                    throw new Exception("Unknown animal companion.");
            }
            Creature animalCompanion = creature1;
            animalCompanion.PostConstructorInitialization(TBattle.Pseudobattle);
            return animalCompanion;
        }

        private static Creature AddSupportAction(Creature companion, string flavorText, string rulesText, Action<QEffect> adjustSupportQEffect)
        {
            companion.AddQEffect(new QEffect()
            {
                ProvideMainAction = (qfSupportAction) => new ActionPossibility(new CombatAction(companion, companion.Illustration, "Support", Array.Empty<Trait>(), "{i}" + flavorText + "{/i}\n\n" + rulesText +
                "\n\n{b}Special{/b} If the animal uses the Support action, the only other actions it can use on this turn are basic move actions; if it has already used any other action this turn, it can't Support you.",
                Target.Self().WithAdditionalRestriction((self) => !self.Actions.ActionHistoryThisTurn.Any((act) => !act.HasTrait(Trait.Move)) ? null : "You already took a non-move action this turn."))
                {
                    ShortDescription = rulesText
                }.WithEffectOnSelf((self) =>
                {
                    QEffect qEffect = new QEffect("Support", rulesText, ExpirationCondition.ExpiresAtStartOfSourcesTurn, GetRanger(companion), companion.Illustration)
                    {
                        DoNotShowUpOverhead = true,
                        PreventTakingAction = (ca) => !ca.HasTrait(Trait.Move) && ca.ActionId != ActionId.EndTurn ? "You used Support so you can't take non-move actions anymore this turn." : null
                    };
                    adjustSupportQEffect(qEffect);
                    companion.AddQEffect(qEffect);
                }))
            });
            return companion;
        }
    }
}
