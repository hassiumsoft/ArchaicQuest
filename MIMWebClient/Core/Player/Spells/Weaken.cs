﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MIMWebClient.Core.Player.Skills
{

    using MIMWebClient.Core.Events;
    using MIMWebClient.Core.PlayerSetup;
    using MIMWebClient.Core.Room;
    using System.Threading.Tasks;

    public class Weaken : Skill
    {
        private static bool _taskRunnning = false;
        private static Player _target = new Player();
        public static Skill WeakenSkill { get; set; }

        public static void StartWeaken(Player player, Room room, string target = "")
        {
            //Check if player has spell
            var hasSpell = Skill.CheckPlayerHasSkill(player, WeakenAb().Name);

            if (hasSpell == false)
            {
                HubContext.Instance.SendToClient("You don't know that spell.", player.HubGuid);
                return;
            }

            var canDoSkill = Skill.CanDoSkill(player);

            if (!canDoSkill)
            {
                return;
            }

            _target = Skill.FindTarget(target, room);

            //Fix issue if target has similar name to user and they use abbrivations to target them
            if (_target == player)
            {
                _target = null;
            }


            if (!_taskRunnning && _target != null)
            {


                if (_target.Effects.FirstOrDefault(x => x.Name.Equals("Weaken")) != null)
                {
                    HubContext.Instance.SendToClient("They are already weaken.", player.HubGuid);
                    return;
                }


                if (player.ManaPoints < WeakenAb().ManaCost)
                {
                    HubContext.Instance.SendToClient("You fail to concentrate due to lack of mana.", player.HubGuid);

                    return;
                }

                //TODO REfactor

                player.ManaPoints -= WeakenAb().ManaCost;

                Score.UpdateUiPrompt(player);

                HubContext.Instance.SendToClient("You utter nequaquam multus.", player.HubGuid);

                foreach (var character in room.players)
                {
                    if (character != player)
                    {
                        var roomMessage = $"{ Helpers.ReturnName(player, character, string.Empty)} utters nequaquam multus.";

                        HubContext.Instance.SendToClient(roomMessage, character.HubGuid);
                    }
                }

                Task.Run(() => DoWeaken(player, room));

            }
            else
            {
                if (_target == null)
                {

                    HubContext.Instance.SendToClient("You need to cast Weaken on a target", player.HubGuid);

                }



            }

        }

        private static async Task DoWeaken(Player attacker, Room room)
        {
            _taskRunnning = true;
            attacker.Status = Player.PlayerStatus.Busy;


            await Task.Delay(500);


            var castingTextAttacker =
                   Helpers.ReturnName(_target, attacker, null) + "'s muscles shrink making them look weaker.";

            var castingTextDefender = "You feel weaker as your muscles shrink.";

            HubContext.Instance.SendToClient(castingTextAttacker, attacker.HubGuid);
            HubContext.Instance.SendToClient(castingTextDefender, _target.HubGuid);

            foreach (var character in room.players)
            {
                if (character == attacker)
                {
                    continue;
                }

                if (character != _target)
                {
 
                    var roomMessage = $"{Helpers.ReturnName(_target, character, string.Empty)}'s  muscles shrink making them look weaker.";

                    HubContext.Instance.SendToClient(roomMessage, character.HubGuid);
                }
            }

            _target.Strength -= 2;


            var weakenAff = new Effect
            {
                Name = "Weaken",
                Duration = attacker.Level + 5,
                AffectLossMessagePlayer = "Your muscles regain there mass and strength.",
                AffectLossMessageRoom = $" muscles regain there mass and strength."
            };


            if (_target.Effects == null)
            {
                _target.Effects = new List<Effect>();
                _target.Effects.Add(weakenAff);

            }
            else
            {
                _target.Effects.Add(weakenAff);
            }

            Score.ReturnScoreUI(_target);
            Score.UpdateUiAffects(_target);

            Player.SetState(attacker);
            _target = null;
            _taskRunnning = false;


        }

        public static Skill WeakenAb()
        {


            var skill = new Skill
            {
                Name = "Weaken",
                SpellGroup = SpellGroupType.Illusion,
                SkillType = Type.Spell,
                CoolDown = 0,
                Delay = 0,
                LevelObtained = 1,
                ManaCost = 10,
                Passive = false,
                Proficiency = 1,
                MaxProficiency = 95,
                UsableFromStatus = "Standing",
                Syntax = "cast weaken <Target>"
            };


            var help = new Help
            {
                Syntax = skill.Syntax,
                HelpText = "Decreases strength by 2",
                DateUpdated = "16/04/2017"

            };

            skill.HelpText = help;


            return skill;


        }
    }
}