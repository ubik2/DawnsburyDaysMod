using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Core;

namespace Dawnsbury.Mods.Remaster.FeatsDb.TrueFeatsDb
{
    public static class Fighter
    {
        public static IEnumerable<Feat> LoadAll()
        {
            yield return new TrueFeat(RemasterFeats.FeatName.SlamDown, 4, "You make an attack to knock a foe off balance, then follow up immediately with a sweep to topple them.",
                                     "Make a melee Strike. If it hits and deals damage, you can attempt an Athletics check to Trip the creature you hit. If you’re wielding a two-handed melee weapon, you can ignore Trip's requirement that you have a hand free. Both attacks count toward your multiple attack penalty, but the penalty doesn't increase until after you've made both of them.",
                                     new[] { Trait.Fighter, Trait.Flourish })
                .WithActionCost(2)
                .WithPrerequisite((CalculatedCharacterSheetValues sheet) => sheet.GetProficiency(Trait.Athletics) >= Proficiency.Trained, "You must be trained in Athletics.")
                .WithPermanentQEffect("You make an attack to knock a foe off balance, then follow up immediately with a sweep to topple them.", delegate (QEffect caster)
                {
                    caster.ProvideStrikeModifier = delegate (Item item)
                    {
                        CombatAction combatAction = caster.Owner.CreateStrike(item).WithActionCost(2);
                        combatAction.Traits.Add(Trait.Flourish);
                        combatAction.Illustration = new SideBySideIllustration(combatAction.Illustration, IllustrationName.Trip);
                        combatAction.Name = "Slam Down";
                        combatAction.Description = StrikeRules.CreateBasicStrikeDescription(combatAction.StrikeModifiers, null, "You can attempt an Athletics check to Trip the creature you hit.", "You can attempt an Athletics check to Trip the creature you hit.");
                        StrikeModifiers strikeModifiers = combatAction.StrikeModifiers;
                        strikeModifiers.OnEachTarget = (Func<Creature, Creature, CheckResult, Task>)Delegate.Combine(strikeModifiers.OnEachTarget, (Func<Creature, Creature, CheckResult, Task>)async delegate (Creature caster, Creature target, CheckResult checkResult)
                        {
                            // TODO: also need to check to see if we do damage
                            if (checkResult >= CheckResult.Success)
                            {
                                CombatAction tripAction = Possibilities.CreateTrip(caster);
                                tripAction.ChosenTargets = new ChosenTargets
                                {
                                    ChosenCreature = target
                                };
                                await tripAction.WithActionCost(0).AllExecute();
                            }
                        });
                        ((CreatureTarget)combatAction.Target).WithAdditionalConditionOnTargetCreature((Creature self, Creature target) => (!self.HasFreeHand && !item.HasTrait(Trait.TwoHanded)) ? Usability.CommonReasons.NoFreeHandForManeuver : Usability.Usable);
                        return combatAction;
                    };
                });
        }
    }
}
