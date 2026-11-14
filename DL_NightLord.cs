using System;
using DBDef;
using Thea2.Common;
using Thea2.Server;

namespace GameScript
{
    public class DL_NightLord : ScriptBase
    {
        private static Character FindLord(AdventureEventData data)
        {
            var group = data.GetMainPlayerGroup();
            if (group == null || group.abstractGroup)
                return null;
            foreach (var reference in group.characters)
            {
                var character = reference.Get();
                if (character != null && character.subrace.Get().dbName == "SUBRACE-DL_DERICTOR")
                    return character;
            }
            return null;
        }

        public static void FMO_DL_RollWolfPact(NOAdventureStatus status, AdventureEventData data, LogicModifier modifier, AdvNode node)
        {
            var player = GameInstance.Get().GetPlayer(data.GetMainPlayerID());
            var outcome = Globals.GetInstanceFromDB<Tag>("TAG-DL_WOLF_PACT_OUTCOME");
            if (player == null || outcome == null || player.attributes.GetBase(outcome).ToFloat() != 0)
                return;

            var lord = FindLord(data);
            if (lord == null)
                return;

            int luck = Math.Max(0, Math.Min(10, (int)lord.attributes.GetFinal((Tag)TAG.LUCK).ToFloat()));
            int roll = UnityEngine.Random.Range(0, 100);
            int resultNode = roll < 15 + luck ? 3 : roll < 50 + luck ? 4 : 5;
            player.attributes.SetBaseTo(outcome, (float)resultNode);
        }

        public static bool FRA_DL_WolfOutcome(AdventureEventData data, LogicResultAction action, AdvNode node)
        {
            var player = GameInstance.Get().GetPlayer(data.GetMainPlayerID());
            var outcome = Globals.GetInstanceFromDB<Tag>("TAG-DL_WOLF_PACT_OUTCOME");
            float resultNode = player != null && outcome != null ? player.attributes.GetBase(outcome).ToFloat() : 0;
            return int.Parse(action.scriptCallWithTag.parameter) == (resultNode == 0 ? 5 : resultNode);
        }

        private static Tag CostTag(string pool)
        {
            return pool == "VIDA" ? (Tag)TAG.HEALTH_PHYSICAL : (Tag)TAG.HEALTH_SPIRIT;
        }

        private static bool CanPay(Character lord, string pool, int cost)
        {
            return lord != null && (cost == 0 || lord.attributes.GetBase(CostTag(pool)).ToFloat() > cost);
        }

        public static bool FRA_DL_NightOffer(AdventureEventData data, LogicResultAction action, AdvNode node)
        {
            var player = GameInstance.Get().GetPlayer(data.GetMainPlayerID());
            var progress = Globals.GetInstanceFromDB<Tag>(action.scriptCallWithTag.tag);
            var offer = action.scriptCallWithTag.parameter.Split(',');
            return player != null && progress != null &&
                player.attributes.GetBase(progress).ToFloat() == int.Parse(offer[0]) &&
                CanPay(FindLord(data), offer[1], int.Parse(offer[2]));
        }

        public static void FMO_DL_NightReward(NOAdventureStatus status, AdventureEventData data, LogicModifier modifier, AdvNode node)
        {
            var group = data.GetMainPlayerGroup();
            var lord = FindLord(data);
            var player = GameInstance.Get().GetPlayer(data.GetMainPlayerID());
            var progress = Globals.GetInstanceFromDB<Tag>(modifier.scriptCallWithTag.tag);
            var offer = modifier.scriptCallWithTag.parameter.Split(',');
            int stage = int.Parse(offer[0]);
            int cost = int.Parse(offer[2]);
            if (player == null || progress == null || player.attributes.GetBase(progress).ToFloat() != stage ||
                !CanPay(lord, offer[1], cost))
                return;

            AdventureEventDataChange change = null;
            if (offer[3].StartsWith("ITEM_CARGO-", StringComparison.Ordinal))
            {
                var cargo = Globals.GetInstanceFromDB<ItemCargo>(offer[3]);
                if (cargo == null)
                    return;
                change = ItemBase.InstantaiteFromToGroup(cargo, group, 1f, 1);
                if (change == null)
                    return;
            }
            else if (offer[3].StartsWith("SUBRACE-", StringComparison.Ordinal))
            {
                var form = Globals.GetInstanceFromDB<Subrace>(offer[3]);
                if (form == null)
                    return;
                var character = Character.Instantiate(group, form, 1, false);
                change = new AdventureEventDataChange();
                change.GatherReferenceInfo(character);
                change.count = 1;
            }

            if (cost > 0)
                lord.attributes.AddToBase(CostTag(offer[1]), -cost);
            player.attributes.SetBaseTo(progress, (float)(stage + 1));
            if (change != null)
            {
                change.playerID = group.ownerID;
                data.AddUncollectedAdventureChange(change);
            }
        }
    }
}
