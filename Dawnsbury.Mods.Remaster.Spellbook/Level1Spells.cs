using Microsoft.Xna.Framework;
using Dawnsbury.Audio;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.Animations;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Modding;
using Dawnsbury.Display.Text;
using Dawnsbury.Core.Mechanics.Damage;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Roller;
using Dawnsbury.Core.Mechanics.Treasure;
using Humanizer;
using Dawnsbury.Core.StatBlocks;
using Dawnsbury.Display;

namespace Dawnsbury.Mods.Remaster.Spellbook
{
    public static class Level1Spells
    {
        // The following spells are excluded because they aren't useful enough in gameplay
        // * Air Bubble
        // * Alarm
        // * Ant Haul
        // * Charm
        // * Cleanse Cuisine (formerly Purify Food and Drink)
        // * Create Water
        // * Disguise Magic
        // * Gentle Landing (formerly Feather Fall)
        // * Illusory Disguise
        // * Illusory Object
        // * Item Facade
        // * Jump
        // * Lock
        // * Mending
        // * Mindlink
        // * Pest Form - this was replaced with Insect Form, since the exploration aspects aren't useful
        // * Pet Cache
        // * Sleep
        // * Tailwind (formerly Longstrider) - Fleet Step is generally better in game, but we could give this the mage armor treatment for level 2
        // * Vanishing Tracks
        // * Ventriloquism
        // The following spell is moved to the Level2Spells since there are no creatures available at rank 1
        // * Summon Construct
        // * Summon Plant or Fungus
        // The following spells are excluded because of their difficulty
        // * Ill Omen
        // * Phantasmal Minion
        // * Summon Fey - no creatures exist with this trait
        // The following are in limbo
        // ? Gust of Wind
        // ? Harm/Heal - these are already in game, but could be modified for vitality/void instead of positive/negative
        // ? Infuse Vitality - should be able to hook into YouDealDamageWithStrike
        // ? Spirit Link
        public static void RegisterSpells()
        {
            ModManager.ReplaceExistingSpell(SpellId.Bless, 1, (spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                return Bless(spellLevel, inCombat, IllustrationName.Bless, true);
            });
            ModManager.ReplaceExistingSpell(SpellId.Bane, 1, (spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                return Bless(spellLevel, inCombat, IllustrationName.Bane, false);
            });

            // Renamed from Burning Hands. Updated traits and description.
            RemasterSpells.ReplaceLegacySpell(SpellId.BurningHands, "BreatheFire", 1, (spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                return Spells.CreateModern(IllustrationName.BurningHands, "Breathe Fire", new[] { Trait.Concentrate, Trait.Fire, Trait.Manipulate, Trait.Arcane, Trait.Primal, RemasterSpells.Trait.Remaster },
                    "A gout of flame sprays from your mouth.",
                    "You deal " + S.HeightenedVariable(2 * spellLevel, 2) + "d6 fire damage to creatures in the area with a basic Reflex save." +
                    S.HeightenedDamageIncrease(spellLevel, inCombat, "2d6"),
                    Target.FifteenFootCone(), spellLevel, SpellSavingThrow.Basic(Defense.Reflex)).WithSoundEffect(SfxName.Fireball).WithGoodnessAgainstEnemy((Target t, Creature a, Creature d) => (float)(t.OwnerAction.SpellLevel * 2) * 3.5f)
                .WithEffectOnEachTarget(async (CombatAction spell, Creature caster, Creature target, CheckResult checkResult) =>
                {
                    await CommonSpellEffects.DealBasicDamage(spell, caster, target, checkResult, 2 * spellLevel + "d6", DamageKind.Fire);
                });
            });

            // Renamed from Color Spray. Updated traits and short description.
            RemasterSpells.ReplaceLegacySpell(SpellId.ColorSpray, "DizzyingColors", 1, (spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                return Spells.CreateModern(IllustrationName.ColorSpray, "Dizzying Colors", new[] { Trait.Concentrate, Trait.Illusion, Trait.Incapacitation, Trait.Manipulate, Trait.Visual, Trait.Arcane, Trait.Occult, RemasterSpells.Trait.Remaster },
                    "You unleash a swirling multitude of colors that overwhelms creatures based on their Will saves.",
                    "Each target makes a Will save.\n\n{b}Critical success{/b} The creature is unaffected.\n{b}Success{/b} The creature is dazzled for 1 round.\n{b}Failure{/b} The creature is stunned 1, blinded for 1 round, and dazzled for the rest of the encounter.\n{b}Critical failure{/b} The creature is stunned for 1 round and blinded for the rest of the encounter.",
                    Target.FifteenFootCone(), spellLevel, SpellSavingThrow.Standard(Defense.Will)).WithSoundEffect(SfxName.MagicMissile).WithProjectileCone(IllustrationName.Pixel, 25, ProjectileKind.ColorSpray)
                .WithGoodness((Target t, Creature a, Creature d) => a.AI.ColorSpray(d))
                .WithEffectOnEachTarget(async (CombatAction spell, Creature caster, Creature target, CheckResult checkResult) =>
                {
                    switch (checkResult)
                    {
                        case CheckResult.Success:
                            target.AddQEffect(QEffect.Dazzled().WithExpirationAtStartOfSourcesTurn(caster, 1));
                            break;
                        case CheckResult.Failure:
                            target.AddQEffect(QEffect.Dazzled().WithExpirationNever());
                            target.AddQEffect(QEffect.Blinded().WithExpirationAtStartOfSourcesTurn(caster, 1));
                            target.AddQEffect(QEffect.Stunned(1));
                            break;
                        case CheckResult.CriticalFailure:
                            target.AddQEffect(QEffect.Blinded().WithExpirationNever());
                            target.AddQEffect(QEffect.Stunned(3));
                            break;
                    }
                });
            });

            // Ray of Enfeeblement wasn't included, and this remastered version is useful.
            RemasterSpells.RegisterNewSpell("Enfeeble", 1, (spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                return Spells.CreateModern(IllustrationName.Enfeebled, "Enfeeble", new[] { Trait.Concentrate, Trait.Manipulate, Trait.Arcane, Trait.Divine, Trait.Occult, RemasterSpells.Trait.Remaster },
                    "You sap the target's strength, depending on its Fortitude save.",
                    RemasterSpells.StripInitialWhitespace(S.FourDegreesOfSuccess("The target is unaffected.", "The target is enfeebled 1 until the start of your next turn.",
                                           "The target is enfeebled 2 for 1 minute.", "The target is enfeebled 3 for 1 minute.")),
                    Target.Ranged(6), spellLevel, SpellSavingThrow.Standard(Defense.Fortitude)).WithSoundEffect(SfxName.Necromancy)
                .WithEffectOnEachTarget(async (CombatAction spell, Creature caster, Creature target, CheckResult checkResult) =>
                {
                    switch (checkResult)
                    {
                        case CheckResult.Success:
                            target.AddQEffect(QEffect.Enfeebled(1).WithExpirationAtStartOfSourcesTurn(caster, 1));
                            break;
                        case CheckResult.Failure:
                            target.AddQEffect(QEffect.Enfeebled(2).WithExpirationNever());
                            break;
                        case CheckResult.CriticalFailure:
                            target.AddQEffect(QEffect.Enfeebled(3).WithExpirationNever());
                            break;
                    }
                });
            });

            // Renamed from Magic Missile. Updated traits and short description.
            RemasterSpells.ReplaceLegacySpell(SpellId.MagicMissile, "ForceBarrage", 1, (spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                Func<CreatureTarget> func = () => Target.Ranged(24, (Target tg, Creature attacker, Creature defender) => attacker.AI.DealDamage(defender, 3.5f, tg.OwnerAction));
                return Spells.CreateModern(IllustrationName.MagicMissile, "Force Barrage", new[] { Trait.Concentrate, Trait.Force, Trait.Manipulate, Trait.Arcane, Trait.Occult, RemasterSpells.Trait.Remaster },
                    "You fire a shard of solidified magic toward a creature that you can see",
                    "{b}Range{/b} 120 feet\n{b}Targets{/b} 1, 2 or 3 creatures\n\nYou send up to three darts of force. They each automatically hit and deal 1d4+1 force damage. {i}(All darts against a single target count as a single damage event.){/i}\n\nYou can spend 1–3 actions on this spell:\n{icon:Action} You send out 1 dart.\n{icon:TwoActions}You send out 2 darts.\n{icon:ThreeActions}You send out 3 darts.{/i}",
                    Target.DependsOnActionsSpent(Target.MultipleCreatureTargets(func()).WithOverriddenTargetLine("1 creature", plural: false), Target.MultipleCreatureTargets(func(), func()).WithOverriddenTargetLine("1 or 2 creatures", plural: true), Target.MultipleCreatureTargets(func(), func(), func()).WithOverriddenTargetLine("1, 2 or 3 creatures", plural: true)), spellLevel, null).WithActionCost(-1).WithSoundEffect(SfxName.MagicMissile)
                .WithProjectileCone(IllustrationName.MagicMissile, 15, ProjectileKind.Ray)
                .WithCreateVariantDescription((int actionCost, SpellVariant? variant) => (actionCost != 1) ? ("You send out " + actionCost + " darts of force. They each automatically hit and deal 1d4+1 force damage. {i}(All darts against a single target count as a single damage event.)") : "You send out 1 dart of force. It automatically hits and deals 1d4+1 force damage.")
                .WithEffectOnChosenTargets(async (CombatAction action, Creature caster, ChosenTargets targets) =>
                {
                    List<Task> list = new List<Task>();
                    foreach (Creature chosenCreature in targets.ChosenCreatures)
                    {
                        list.Add(caster.Battle.SpawnOverairProjectileParticlesAsync(10, caster.Occupies, chosenCreature.Occupies, Color.White, IllustrationName.MagicMissile));
                    }

                    await Task.WhenAll(list);
                    Dictionary<Creature, int> dictionary = new Dictionary<Creature, int>();
                    foreach (Creature chosenCreature2 in targets.ChosenCreatures)
                    {
                        if (!dictionary.TryAdd(chosenCreature2, 1))
                        {
                            dictionary[chosenCreature2]++;
                        }
                    }

                    foreach (KeyValuePair<Creature, int> item4 in dictionary)
                    {
                        List<DiceFormula> list2 = new List<DiceFormula>();
                        for (int i = 0; i < item4.Value; i++)
                        {
                            list2.Add(DiceFormula.FromText("1d4+1", "Magic missile"));
                        }

                        await caster.DealDirectDamage(new DamageEvent(action, item4.Key, CheckResult.Success, list2.Select((DiceFormula formula) => new KindedDamage(formula, DamageKind.Force)).ToArray()));
                    }
                })
                .WithTargetingTooltip((CombatAction power, Creature creature, int index) =>
                {
                    string text7 = index switch
                    {
                        0 => "first",
                        1 => "second",
                        2 => "third",
                        _ => index + "th",
                    };
                    return "Send the " + text7 + " magic missile at " + creature?.ToString() + ". (" + (index + 1) + "/" + power.SpentActions + ")";
                });
            });

            // Goblin Pox
            RemasterSpells.RegisterNewSpell("GoblinPox", 1, (spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                return Spells.CreateModern(IllustrationName.SuddenBlight, "Goblin Pox", new[] { Trait.Concentrate, RemasterSpells.Trait.Disease, Trait.Manipulate, Trait.Arcane, Trait.Primal, RemasterSpells.Trait.Remaster },
                    "Your touch afflicts the target with goblin pox, an irritating allergenic rash.",
                    "The target must attempt a Fortitude save. " +
                    S.FourDegreesOfSuccess("The target is unaffected.", "The target is sickened 1.",
                                           "The target is afflicted with goblin pox at stage 1.", "The target is afflicted with goblin pox at stage 2."),
                    Target.AdjacentCreature(), spellLevel, SpellSavingThrow.Standard(Defense.Fortitude)).WithSoundEffect(SfxName.Necromancy)
                .WithEffectOnEachTarget(async (CombatAction spell, Creature caster, Creature target, CheckResult checkResult) =>
                {
                    if (spell.SpellcastingSource == null)
                    {
                        throw new Exception("Spellcasting source is null");
                    }
                    int afflictionLevel = checkResult switch
                    {
                        CheckResult.CriticalSuccess => 0,
                        CheckResult.Success => 0,
                        CheckResult.Failure => 1,
                        CheckResult.CriticalFailure => 2,
                        _ => throw new Exception("Unknown result"),
                    };
                    if (afflictionLevel == 0)
                    {
                        if (checkResult == CheckResult.Success)
                        {
                            target.AddQEffect(QEffect.Sickened(1, spell.SpellcastingSource.GetSpellSaveDC()));
                        }
                    }
                    else
                    {
                        Affliction affliction = GoblinPoxAffliction(caster, spell.SpellcastingSource.GetSpellSaveDC());
                        CombatAction diseaseAction = new CombatAction(caster, IllustrationName.BadUnspecified, affliction.Name, new[] { RemasterSpells.Trait.Disease }, "", Target.Self());
                        await target.AddAffliction(affliction.MaximumStage, EnterStage, new QEffect(affliction.Name + ", Stage", affliction.StagesDescription, ExpirationCondition.Never, caster, IllustrationName.AcidSplash)
                        {
                            Id = affliction.Id,
                            Value = afflictionLevel,
                            StateCheck = affliction.StateCheck,
                            StartOfSourcesTurn = async (QEffect qfAffliction) =>
                            {
                                CheckResult startOfTurnResult = CommonSpellEffects.RollSavingThrow(target, diseaseAction, Defense.Fortitude, (Creature? _) => affliction.DC);
                                Affliction.AdjustValue(qfAffliction, startOfTurnResult, affliction.MaximumStage);
                                if (qfAffliction.Value > 0)
                                {
                                    await EnterStage(qfAffliction);
                                }
                            }
                        }.WithExpirationAtStartOfSourcesTurn(caster, 1));
                        async Task EnterStage(QEffect qEffect)
                        {
                            // This is strange, but it seems to do the right thing. We'll trigger the StartOfSourcesTurn delegate on the next turn.
                            qEffect.WithExpirationAtStartOfSourcesTurn(caster, 2);
                            return;
                        }
                    }
                });
            });

            // Mystic Armor (formerly Mage Armor)
            RemasterSpells.ReplaceLegacySpell(SpellId.MageArmor, "MysticArmor", 1, (spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                CombatAction mageArmor = Spells.CreateModern(IllustrationName.MageArmor, "Mystic Armor", new[] { Trait.Concentrate, Trait.Manipulate, Trait.Arcane, Trait.Divine, Trait.Occult, Trait.Primal, RemasterSpells.Trait.Remaster },
                    "You ward yourself with shimmering magical energy, gaining a +1 item bonus to AC and a maximum Dexterity modifier of +5.",
                    "While wearing mystic armor, you use your unarmored proficiency to calculate your AC." +
                    "\n\n{b}Special{/b} You can cast this spell as a free action at the beginning of the encounter.", Target.Self().WithAdditionalRestriction((Creature self) => (!self.HasEffect(QEffectId.MageArmor) && !self.PersistentUsedUpResources.CastMageArmor) ? null : "You're already wearing {i}mystic armor{/i}."), spellLevel, null).WithSoundEffect(SfxName.Abjuration).WithActionCost(2)
                .WithEffectOnEachTarget(async (CombatAction spell, Creature caster, Creature target, CheckResult checkResult) =>
                {
                    caster.AddQEffect(QEffect.MageArmor());
                    caster.PersistentUsedUpResources.CastMageArmor = true;
                });
                mageArmor.WhenCombatBegins = (Creature self) =>
                {
                    self.AddQEffect(new QEffect
                    {
                        StartOfCombat = async (_) =>
                        {
                            if (!self.PersistentUsedUpResources.CastMageArmor && await self.Battle.AskForConfirmation(self, IllustrationName.MageArmor, "Do you want to cast {i}mage armor{/i} as a free action?", "Cast {i}mage armor{/i}"))
                            {
                                await self.Battle.GameLoop.FullCast(mageArmor);
                            }
                        }
                    });
                };
                return mageArmor;
            });

            // Phantom Pain
            RemasterSpells.RegisterNewSpell("PhantomPain", 1, (spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                return Spells.CreateModern(IllustrationName.MageArmor, "Phantom Pain", new[] { Trait.Concentrate, Trait.Illusion, Trait.Manipulate, Trait.Mental, Trait.Nonlethal, Trait.Occult, RemasterSpells.Trait.Remaster },
                    "Illusory pain wracks the target, dealing " + S.HeightenedVariable(2 * spellLevel, 2) + "d4 mental damage and " + S.HeightenedVariable(spellLevel, 1) + "d4 persistent mental damage with a Will save.",
                    RemasterSpells.StripInitialWhitespace(S.FourDegreesOfSuccess("The target is unaffected.",
                                           "The target takes full initial damage but no persistent damage, and the spell ends immediately.",
                                           "The target takes full initial and persistent damage, and the target is sickened 1. If the target recovers from being sickened, the persistent damage ends and the spell ends.",
                                           "As failure, but the target is sickened 2.")),
                    Target.Ranged(6), spellLevel, SpellSavingThrow.Standard(Defense.Will))
                .WithEffectOnEachTarget(async (CombatAction spell, Creature caster, Creature target, CheckResult checkResult) =>
                {
                    if (checkResult != CheckResult.CriticalSuccess)
                    {
                        await CommonSpellEffects.DealBasicDamage(spell, caster, target, checkResult, (2 * spellLevel) + "d4", DamageKind.Mental);
                        if ((checkResult == CheckResult.Failure || checkResult == CheckResult.CriticalFailure) && (spell.SpellcastingSource != null))
                        {
                            QEffect persistentDamage = QEffect.PersistentDamage(spellLevel + "d4", DamageKind.Mental);
                            QEffect sickenedEffect = QEffect.Sickened(checkResult == CheckResult.CriticalFailure ? 2 : 1, spell.SpellcastingSource.GetSpellSaveDC());
                            sickenedEffect.WhenExpires = (_) => { sickenedEffect.Owner.RemoveAllQEffects(qEffect => qEffect == persistentDamage); };
                            target.AddQEffect(persistentDamage);
                            target.AddQEffect(sickenedEffect);
                        }
                    }
                });
            });

            // Protection
            ModManager.ReplaceExistingSpell(SpellId.Protection, 1, (spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                return Spells.CreateModern(IllustrationName.ForbiddingWard, "Protection", new[] { Trait.Concentrate, Trait.Manipulate, Trait.Divine, Trait.Occult, RemasterSpells.Trait.Remaster },
                    "You ward a creature against harm.",
                    "The target gains a +1 status bonus to Armor Class and saving throws.",
                    Target.AdjacentFriendOrSelf(), spellLevel, null)
                .WithEffectOnEachTarget(async (CombatAction spell, Creature caster, Creature target, CheckResult checkResult) =>
                {
                    // As usual for Dawnsbury, we treat 1 minute as not expiring.
                    target.AddQEffect(new QEffect("Protection", "You have a +1 status bonus to Armor Class and saving throws.", ExpirationCondition.Never, null, IllustrationName.ForbiddingWard)
                    {
                        DoNotShowUpOverhead = true,
                        BonusToDefenses = (QEffect _, CombatAction? _, Defense defense) => (CheckDefense(defense) ? new Bonus(1, BonusType.Status, "Protection") : null),
                        CountsAsABuff = true
                    });
                });

                // Helper function
                bool CheckDefense(Defense defense)
                {
                    switch (defense)
                    {
                        case Defense.AC:
                        case Defense.Fortitude:
                        case Defense.Reflex:
                        case Defense.Will:
                            return true;
                        default:
                            return false;
                    }
                }
            });

            // Runic Body (formerly Magic Fang)
            RemasterSpells.RegisterNewSpell("RunicBody", 1, (spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                static bool IsValidTargetForRunicBody(Item? item)
                {
                    if (item != null && item.HasTrait(Trait.Unarmed) && item.WeaponProperties != null)
                    {
                        if (item.WeaponProperties.DamageDieCount > 1)
                        {
                            return item.WeaponProperties.ItemBonus <= 1;
                        }
                        return true;
                    }
                    return false;
                }

                return Spells.CreateModern(IllustrationName.KineticRam, "Runic Body", new[] { Trait.Concentrate, Trait.Manipulate, Trait.Arcane, Trait.Divine, Trait.Occult, Trait.Primal, RemasterSpells.Trait.Remaster },
                    "Glowing runes appear on the target’s body.",
                    "All its unarmed attacks become +1 striking unarmed attacks, gaining a +1 item bonus to attack rolls and increasing the number of damage dice to two.",
                    Target.AdjacentFriendOrSelf()
                .WithAdditionalConditionOnTargetCreature((Creature a, Creature d) => IsValidTargetForRunicBody(d.UnarmedStrike) ? Usability.Usable : Usability.CommonReasons.TargetIsNotPossibleForComplexReason), spellLevel, null)
                .WithSoundEffect(SfxName.MagicWeapon)
                .WithEffectOnEachTarget(async (CombatAction spell, Creature caster, Creature target, CheckResult checkResult) =>
                {
                    Item? item = target.UnarmedStrike;
                    if (item != null && item.WeaponProperties != null)
                    {
                        item.WeaponProperties.DamageDieCount = 2;
                        item.WeaponProperties.ItemBonus = 1;
                    }
                    // Expiration is long enough that we don't need to worry about restoring the item.
                    // I create a buff icon, since otherwise it's not clear that your fist is buffed.
                    target.AddQEffect(new QEffect("Runic Body", "Glowing runes appear on the target’s body.") { Illustration = IllustrationName.KineticRam, CountsAsABuff = true });
                });
            });

            // Runic Weapon (formerly Magic Weapon)
            RemasterSpells.ReplaceLegacySpell(SpellId.MagicWeapon, "RunicWeapon", 1, (spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                static bool IsValidTargetForMagicWeapon(Item item)
                {
                    if (item.HasTrait(Trait.Weapon) && item.WeaponProperties != null)
                    {
                        if (item.WeaponProperties.DamageDieCount > 1)
                        {
                            return item.WeaponProperties.ItemBonus <= 1;
                        }
                        return true;
                    }
                    return false;
                }

                return Spells.CreateModern(IllustrationName.MagicWeapon, "Runic Weapon", new[] { Trait.Concentrate, Trait.Manipulate, Trait.Arcane, Trait.Divine, Trait.Occult, Trait.Primal, RemasterSpells.Trait.Remaster },
                    "The weapon glimmers with magic as temporary runes carve down its length.",
                    "The target becomes a +1 striking weapon, gaining a +1 item bonus to attack rolls and increasing the number of weapon damage dice to two. The target becomes a +1 striking weapon, gaining a +1 item bonus to attack rolls and increasing the number of weapon damage dice to two.",
                    Target.AdjacentFriendOrSelf()
                .WithAdditionalConditionOnTargetCreature((Creature a, Creature d) => (!d.HeldItems.Any(IsValidTargetForMagicWeapon)) ? Usability.CommonReasons.TargetIsNotMagicWeaponTarget : Usability.Usable), spellLevel, null)
                .WithSoundEffect(SfxName.MagicWeapon)
                .WithEffectOnEachTarget(async (CombatAction spell, Creature caster, Creature target, CheckResult checkResult) =>
                {
                    Item item;
                    switch (target.HeldItems.Count(IsValidTargetForMagicWeapon))
                    {
                        case 0:
                            return;
                        case 1:
                            item = target.HeldItems.First(IsValidTargetForMagicWeapon);
                            break;
                        default:
                            item = (await target.Battle.AskForConfirmation(caster, IllustrationName.MagicWeapon, "Which weapon would you like to enchant?", target.HeldItems[0].Name, target.HeldItems[1].Name)) ? target.HeldItems[0] : target.HeldItems[1];
                            break;
                    }

                    item.Name = "+1 striking " + EnumHumanizeExtensions.Humanize(item.BaseItemName);
                    if (item.WeaponProperties != null)
                    {
                        item.WeaponProperties.DamageDieCount = 2;
                        item.WeaponProperties.ItemBonus = 1;
                    }
                    target.AddQEffect(new QEffect
                    {
                        CountsAsABuff = true
                    });

                });
            });

            // Spider Sting
            RemasterSpells.RegisterNewSpell("SpiderSting", 1, (spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                return Spells.CreateModern(IllustrationName.VenomousSnake256, "Spider Sting", new[] { Trait.Concentrate, Trait.Manipulate, Trait.Poison, Trait.Arcane, Trait.Primal, RemasterSpells.Trait.Remaster },
                    "You magically duplicate a spider's venomous sting.",
                    "You deal 1d4 piercing damage to the touched creature and afflict it with spider venom. The target must attempt a Fortitude save. " +
                    S.FourDegreesOfSuccess("The target is unaffected.", "The target takes 1d4 poison damage.",
                                           "The target is afflicted with spider venom at stage 1.", "The target is afflicted with spider venom at stage 2."),
                    Target.AdjacentCreature(), spellLevel, SpellSavingThrow.Standard(Defense.Fortitude)).WithSoundEffect(SfxName.Necromancy)
                .WithEffectOnEachTarget(async (CombatAction spell, Creature caster, Creature target, CheckResult checkResult) =>
                {
                    if (spell.SpellcastingSource == null)
                    {
                        throw new Exception("Spellcasting source is null");
                    }
                    int afflictionLevel = checkResult switch
                    {
                        CheckResult.CriticalSuccess => 0,
                        CheckResult.Success => 0,
                        CheckResult.Failure => 1,
                        CheckResult.CriticalFailure => 2,
                        _ => throw new Exception("Unknown result"),
                    };
                    if (afflictionLevel == 0)
                    {
                        if (checkResult == CheckResult.Success)
                        {
                            await caster.DealDirectDamage(new DamageEvent(spell, target, checkResult, new[] { new KindedDamage(DiceFormula.FromText("d4", spell.Name), DamageKind.Poison) }, false, false));
                        }
                    }
                    else
                    {
                        Affliction affliction = CreateSpiderVenom(target, spell.SpellcastingSource.GetSpellSaveDC());
                        CombatAction afflictionAction = new CombatAction(caster, IllustrationName.BadUnspecified, affliction.Name, new Trait[1] { Trait.Poison }, "", Target.Self());
                        await target.AddAffliction(affliction.MaximumStage, EnterStage, new QEffect(affliction.Name + ", Stage", affliction.StagesDescription, ExpirationCondition.Never, caster, IllustrationName.AcidSplash)
                        {
                            Id = affliction.Id,
                            Value = afflictionLevel,
                            StateCheck = affliction.StateCheck,
                            StartOfSourcesTurn = async (QEffect qfAffliction) =>
                            {
                                CheckResult startOfTurnResult = CommonSpellEffects.RollSavingThrow(target, afflictionAction, Defense.Fortitude, (Creature? _) => affliction.DC);
                                Affliction.AdjustValue(qfAffliction, startOfTurnResult, affliction.MaximumStage);
                                if (qfAffliction.Value > 0)
                                {
                                    await EnterStage(qfAffliction);
                                }
                            }
                        }.WithExpirationAtStartOfSourcesTurn(caster, 4));
                        async Task EnterStage(QEffect qEffect)
                        {
                            string? text = affliction.PoisonDamage(qEffect.Value);
                            if (text != null)
                            {
                                DiceFormula damage = DiceFormula.FromText(text, spell.Name);
                                await afflictionAction.Owner.DealDirectDamage(afflictionAction, damage, qEffect.Owner, CheckResult.Failure, DamageKind.Poison);
                            }
                            return;
                        }
                    }
                });
            });

            RemasterSpells.RegisterNewSpell("SummonUndead", 1, (spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                int maximumCreatureLevel = spellLevel == 1 ? -1 : 1;
                return Spells.CreateModern(IllustrationName.Skeleton256, "Summon Undead", new[] { Trait.Concentrate, Trait.Manipulate, RemasterSpells.Trait.Summon, Trait.Arcane, Trait.Divine, Trait.Occult, RemasterSpells.Trait.Remaster },
                    "You summon a creature that has the undead trait.", "You summon a creature that has the undead trait and whose level is " + S.HeightenedVariable(maximumCreatureLevel, -1) + " or less to fight for you." + Core.CharacterBuilder.FeatsDb.Spellbook.Level1Spells.SummonRulesText + S.HeightenText(spellLevel, 1, inCombat, "{b}Heightened (2nd){/b} The maximum level of the summoned creature is 1."),
                    Target.RangedEmptyTileForSummoning(6), spellLevel, null).WithActionCost(3).WithSoundEffect(SfxName.Summoning)
                    .WithVariants(MonsterStatBlocks.MonsterExemplars.Where((creature) => creature.HasTrait(Trait.Undead) && creature.Level <= maximumCreatureLevel).Select((creature) => new SpellVariant(creature.Name, "Summon " + creature.Name, creature.Illustration)
                    {
                        GoodnessModifier = (ai, original) => original + (float)(creature.Level * 20)
                    }).ToArray()).WithCreateVariantDescription((_, variant) => RulesBlock.CreateCreatureDescription(MonsterStatBlocks.MonsterExemplarsByName[variant!.Id])).WithEffectOnChosenTargets(async (spell, caster, targets) => await CommonSpellEffects.SummonMonster(spell, caster, targets.ChosenTile!));
            });

            // Sure Strike (formerly True Strike)
            RemasterSpells.ReplaceLegacySpell(SpellId.TrueStrike, "SureStrike", 1, (spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                return Spells.CreateModern(IllustrationName.TrueStrike, "Sure Strike", new[] { Trait.Concentrate, Trait.Fortune, Trait.Arcane, Trait.Occult, RemasterSpells.Trait.Remaster },
                    "A glimpse into the future ensures your next blow strikes true.",
                    "The next time you make an attack roll before the end of your turn, roll the attack twice and use the better result. The attack ignores circumstance penalties to the attack roll and any flat check required due to the target being concealed or hidden.",
                    Target.Self(), spellLevel, null)
                .WithActionCost(1).WithSoundEffect(SfxName.PositivePing)
                .WithEffectOnSelf((Creature self) =>
                {
                    self.AddQEffect(new QEffect("Sure Strike", "The next time you make an attack roll before the end of your turn, roll the attack twice and use the better result. The attack ignores circumstance penalties to the attack roll and any flat check required due to the target being concealed or hidden.", ExpirationCondition.ExpiresAtEndOfSourcesTurn, self, IllustrationName.TrueStrike)
                    {
                        CountsAsABuff = true,
                        Id = QEffectId.TrueStrike,
                        DoNotShowUpOverhead = true,
                        ProvideFortuneEffect = (bool isSavingThrow) => (!isSavingThrow) ? "Sure Strike" : null,
                        AfterYouMakeAttackRoll = (QEffect qfSelf, CheckBreakdownResult result) => { qfSelf.ExpiresAt = ExpirationCondition.Immediately; }
                    });
                });
            });

            // Thunderstrike (formerly Shocking Grasp)
            // The extra effect on targets wearing metal armor or made of metal is not implemented
            RemasterSpells.ReplaceLegacySpell(SpellId.ShockingGrasp, "Thunderstrike", 1, (spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                return Spells.CreateModern(IllustrationName.ShockingGrasp, "Thunderstrike", new[] { Trait.Concentrate, Trait.Electricity, Trait.Manipulate, Trait.Sonic, Trait.Arcane, Trait.Primal, RemasterSpells.Trait.Remaster },
                    "You call down a tendril of lightning that cracks with thunder, dealing " + S.HeightenedVariable(spellLevel, 1) + "d12 electricity damage and " + S.HeightenedVariable(spellLevel, 1) + "d4 sonic damage to the target with a basic Reflex save.",
                    // "A target wearing metal armor or made of metal takes a –1 circumstance bonus to its save, and if damaged by the spell is clumsy 1 for 1 round."
                    S.HeightenedDamageIncrease(spellLevel, inCombat, "1d12 electricity and 1d4 sonic"),
                    Target.Ranged(24), spellLevel, SpellSavingThrow.Standard(Defense.Reflex))
                .WithSoundEffect(SfxName.ShockingGrasp)
                .WithEffectOnEachTarget(async (CombatAction spell, Creature caster, Creature target, CheckResult checkResult) => 
                {
                    await CommonSpellEffects.DealBasicDamage(spell, caster, target, checkResult,
                        new KindedDamage(DiceFormula.FromText(spellLevel + "d12", spell.Name), DamageKind.Electricity),
                        new KindedDamage(DiceFormula.FromText(spellLevel + "d4", spell.Name), DamageKind.Sonic));
                });
            });
        }

        private static Affliction GoblinPoxAffliction(Creature source, int dc)
        {
            // This expiration is unexpected. I don't get called if I don't use the WithExpirationAtStartOfSourcesTurn, since
            // we really only call it when we're about to expire. However, I suspect the decrement and potential removal happens
            // after this is called, so if I use 1 round, it will get removed. Instead, I set it to 2 rounds.
            return new Affliction(QEffectId.SlowingVenom, "Goblin Pox", dc,
                "{b}Stage 1{/b} sickened 1; {b}Stage 2{/b} sickened 1 and slowed 1; {b}Stage 3{/b} sickened 1 and the creature can't reduce its sickened value below 1.", 3, (int stage) => null,
                (QEffect qfDisease) =>
            {
                if (qfDisease.Value == 1)
                {
                    qfDisease.Owner.AddQEffect(QEffect.Sickened(1, dc));
                }
                else if (qfDisease.Value == 2)
                {
                    qfDisease.Owner.AddQEffect(QEffect.Sickened(1, dc));
                    if (!qfDisease.Owner.HasEffect(QEffectId.Slowed))
                    {
                        qfDisease.Owner.AddQEffect(QEffect.Slowed(1).WithExpirationAtStartOfSourcesTurn(source, 1));
                    }
                }
                else if (qfDisease.Value == 3)
                {
                    qfDisease.Owner.AddQEffect(Sickened(1));
                    qfDisease.ExpiresAt = ExpirationCondition.Never;
                }
            });
        }

        // This isn't the same as the Spider Venom defined in the Afflictions
        public static Affliction CreateSpiderVenom(Creature source, int dc)
        {
            return new Affliction(QEffectId.SpiderVenom, "Spider Venom", dc, "{b}Stage 1{/b} 1d4 poison damage and enfeebled 1; {b}Stage 2{/b} 1d4 poison damage and enfeebled 2", 2, (int stage) =>
            {
                switch (stage)
                {
                    case 1:
                    case 2:
                        return "1d4";
                    default:
                        throw new Exception("Unknown stage.");
                }
            }, (QEffect qfPoison) => 
            {
                if (qfPoison.Value == 1)
                {
                    qfPoison.Owner.AddQEffect(QEffect.Enfeebled(1).WithExpirationEphemeral());
                }
                else if (qfPoison.Value == 2)
                {
                    qfPoison.Owner.AddQEffect(QEffect.Enfeebled(2).WithExpirationEphemeral());
                }
            });
        }

        // Get a Sickened effect that can't be reduced.
        private static QEffect Sickened(int value)
        {
            QEffect qEffect = new QEffect("Sickened", "You take a status penalty equal to the value to all your checks and DCs.\n\nYou can't drink elixirs or potions, or be administered elixirs or potions unless you're unconscious.", ExpirationCondition.Never, null, IllustrationName.Sickened)
            {
                Id = QEffectId.Sickened,
                Key = "Sickened",
                Value = value,
                BonusToAllChecksAndDCs = (QEffect qf) => new Bonus(-qf.Value, BonusType.Status, "sickened"),
                PreventTakingAction = (CombatAction ca) => (ca.ActionId != ActionId.Drink) ? null : "You're sickened.",
                CountsAsADebuff = true
            };
            qEffect.PreventTargetingBy = (CombatAction ca) => (ca.ActionId != ActionId.Administer || qEffect.Owner.HasEffect(QEffectId.Unconscious)) ? null : "sickened";
            return qEffect;
        }

        public static CombatAction Bless(int level, bool _inCombat, IllustrationName illustration, bool isBless)
        {
            return Spells.CreateModern(illustration, isBless ? "Bless" : "Bane", new[] { Trait.Aura, Trait.Concentrate, Trait.Manipulate, Trait.Mental, Trait.Divine, Trait.Occult, RemasterSpells.Trait.Remaster },
                isBless ? "Blessings from beyond help your companions strike true." :
                    "You fill the minds of your enemies with doubt.",
                "{b}Area{/b} 15-foot emanation\n\n" +
                (isBless ? "You and your allies gain a +1 status bonus to attack rolls while within the emanation. Once per round on subsequent turns, you can Sustain the spell to increase the emanation's radius by 10 feet. Bless can counteract bane." :
                    "Enemies in the area must succeed at a Will save or take a –1 status penalty to attack rolls as long as they are in the area. Once per round on subsequent turns, you can Sustain the spell to increase the emanation's radius by 10 feet and force enemies in the area that weren't yet affected to attempt another saving throw. Bane can counteract bless."),            
                Target.Self(), level, null).WithSoundEffect(isBless ? SfxName.Bless : SfxName.Fear).WithEffectOnSelf(async (CombatAction action, Creature self) => 
            {
                int initialRadius = isBless ? 3 : 2;
                AuraAnimation auraAnimation = self.AnimationData.AddAuraAnimation(isBless ? IllustrationName.BlessCircle : IllustrationName.BaneCircle, initialRadius);
                QEffect qEffect = new QEffect(isBless ? "Bless" : "Bane", "[this condition has no description]", ExpirationCondition.Never, self, IllustrationName.None)
                {
                    WhenExpires = (_) =>
                    {
                        auraAnimation.MoveTo(0f);
                    },
                    Tag = (initialRadius, true),
                    StartOfYourTurn = async (QEffect qfBless, Creature _) => 
                    {
                        if (qfBless?.Tag != null)
                        {
                            qfBless.Tag = ((((int, bool))qfBless.Tag).Item1, false); 
                        }
                            
                    },
                    ProvideContextualAction = (QEffect qfBless) =>
                    {
                        if (qfBless?.Tag != null)
                        {
                            (int, bool) tag = ((int, bool))qfBless.Tag;
                            return (!tag.Item2) ? new ActionPossibility(new CombatAction(qfBless.Owner, illustration, isBless ? "Increase Bless radius" : "Increase Bane radius", new Trait[1] { Trait.Concentrate }, "Increase the radius of the " + (isBless ? "bless" : "bane") + " emanation by 5 feet.", Target.Self())
                                .WithEffectOnSelf((_) =>
                                {
                                    int newEmanationSize = tag.Item1 + 2;
                                    qfBless.Tag = (newEmanationSize, true);
                                    auraAnimation.MoveTo(newEmanationSize);
                                    if (!isBless)
                                    {
                                        foreach (Creature item in qfBless.Owner.Battle.AllCreatures.Where((Creature cr) => cr.DistanceTo(qfBless.Owner) <= newEmanationSize && cr.EnemyOf(qfBless.Owner)))
                                        {
                                            item.RemoveAllQEffects((QEffect qf) => qf.Id == QEffectId.RolledAgainstBane && qf.Tag == qfBless);
                                        }
                                    }
                                })).WithPossibilityGroup("Maintain an activity") : null;
                        }
                        else
                        {
                            return null;
                        }
                    }
                };
                if (isBless)
                {
                    auraAnimation.Color = Color.Yellow;
                    qEffect.StateCheck = (QEffect qfBless) =>
                    {
                        if (qfBless?.Tag != null)
                        {
                            int emanationSize2 = (((int, bool))qfBless.Tag).Item1;
                            foreach (Creature item2 in qfBless.Owner.Battle.AllCreatures.Where((Creature cr) => cr.DistanceTo(qfBless.Owner) <= emanationSize2 && cr.FriendOf(qfBless.Owner) && !cr.HasTrait(Trait.Mindless) && !cr.HasTrait(Trait.Object)))
                            {
                                item2.AddQEffect(new QEffect("Bless", "You gain a +1 status bonus to attack rolls.", ExpirationCondition.Ephemeral, qfBless.Owner, IllustrationName.Bless)
                                {
                                    CountsAsABuff = true,
                                    BonusToAttackRolls = (QEffect qfBlessed, CombatAction attack, Creature? de) => attack.HasTrait(Trait.Attack) ? new Bonus(1, BonusType.Status, "bless") : null
                                });
                            }
                        }
                    };
                }
                else
                {
                    qEffect.StateCheckWithVisibleChanges = async (QEffect qfBane) =>
                    {
                        if (qfBane?.Tag != null)
                        {

                            int emanationSize = (((int, bool))qfBane.Tag).Item1;
                            foreach (Creature item3 in qfBane.Owner.Battle.AllCreatures.Where((Creature cr) => cr.DistanceTo(qfBane.Owner) <= emanationSize && cr.EnemyOf(qfBane.Owner) && !cr.HasTrait(Trait.Mindless) && !cr.HasTrait(Trait.Object)))
                            {
                                if (!item3.QEffects.Any((QEffect qf) => qf.ImmuneToTrait == Trait.Mental))
                                {
                                    if (item3.QEffects.Any((QEffect qf) => qf.Id == QEffectId.FailedAgainstBane && qf.Tag == qfBane))
                                    {
                                        item3.AddQEffect(new QEffect("Bane", "You take a -1 status penalty to attack rolls.", ExpirationCondition.Ephemeral, qfBane.Owner, IllustrationName.Bane)
                                        {
                                            Key = "BanePenalty",
                                            BonusToAttackRolls = (QEffect qfBlessed, CombatAction attack, Creature? de) => attack.HasTrait(Trait.Attack) ? new Bonus(-1, BonusType.Status, "bane") : null
                                        });
                                    }
                                    else if (!item3.QEffects.Any((QEffect qf) => qf.Id == QEffectId.RolledAgainstBane && qf.Tag == qfBane))
                                    {
                                        CheckResult checkResult = CommonSpellEffects.RollSpellSavingThrow(item3, action, Defense.Will);
                                        item3.AddQEffect(new QEffect(ExpirationCondition.Never)
                                        {
                                            Id = QEffectId.RolledAgainstBane,
                                            Tag = qfBane
                                        });
                                        if (checkResult <= CheckResult.Failure)
                                        {
                                            item3.AddQEffect(new QEffect(ExpirationCondition.Never)
                                            {
                                                Id = QEffectId.FailedAgainstBane,
                                                Tag = qfBane
                                            });
                                        }
                                    }
                                }
                            }
                        }
                    };
                }

                self.AddQEffect(qEffect);
            });
        }
    }
}
