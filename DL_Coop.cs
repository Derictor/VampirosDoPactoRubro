using DBDef;
using Thea2.Common;
using Thea2.Server;

namespace GameScript
{
    public class DL_Coop : ScriptBase
    {
        private const string DeadlineId = "TAG-DL_TURNING_DEADLINE";

        private const string PendingId = "TAG-DL_TURNING";
        private const string EffectMarkerId = "TAG-DL_TURNING_EFFECT";
        private const string WeaknessPackId = "SKILL_PACK-DL_PACT_WEAKNESS";

        [ScriptAttribute(null)]
        public static void FMO_DL_StartTurning(NOAdventureStatus status, AdventureEventData data,
            LogicModifier modifier, AdvNode node)
        {
            var player = GameInstance.Get().GetPlayer(data.GetMainPlayerID());
            var group = data.GetMainPlayerGroup();
            var list = data.GetList(modifier.list, node);
            var pending = Globals.GetInstanceFromDB<Tag>(PendingId);
            var effectMarker = Globals.GetInstanceFromDB<Tag>(EffectMarkerId);
            var deadline = Globals.GetInstanceFromDB<Tag>(DeadlineId);
            var pack = Globals.GetInstanceFromDB<SkillPack>(WeaknessPackId);
            if (player == null || group == null || group.abstractGroup || list == null ||
                pending == null || effectMarker == null || deadline == null || pack == null)
                return;

            foreach (var key in list.GetList())
            {
                var character = list.GetItem(key).item as Character;
                if (character == null || character.group.Get() != group ||
                    character.attributes.Contains(pending, FInt.ONE))
                    continue;

                var effect = character.AddSkillEffect(pack, true, 1);
                if (effect == null)
                    return;

                character.attributes.SetBaseTo(effectMarker, FInt.ONE);
                character.attributes.SetBaseTo(pending, FInt.ONE);
                player.attributes.SetBaseTo(deadline, (float)ServerManager.GetTurnManager().tunrIndex + 5);
                var change = new AdventureEventDataChange();
                change.GatherReferenceInfo(character);
                change.playerID = group.ownerID;
                var block = new AdventureEventDataBlock();
                block.tagDBName = effect.source.dbName;
                block.changeValue = effect.level;
                block.skillData = new NetSkillData(effect.source.dbName, effect.level, 0, effect.GetDelayMultiplier());
                change.advChanges.Add(block);
                data.AddUncollectedAdventureChange(change);
                return;
            }
        }

        [ScriptAttribute(typeof(Subrace))]
        public static void FMO_DL_FinishTurning(NOAdventureStatus status, AdventureEventData data,
            LogicModifier modifier, AdvNode node)
        {
            var group = data.GetMainPlayerGroup();
            var list = data.GetList(modifier.list, node);
            var form = Globals.GetInstanceFromDB<Subrace>(modifier.scriptCallWithTag.tag);
            var pending = Globals.GetInstanceFromDB<Tag>(PendingId);
            var effectMarker = Globals.GetInstanceFromDB<Tag>(EffectMarkerId);
            var pack = Globals.GetInstanceFromDB<SkillPack>(WeaknessPackId);
            if (group == null || group.abstractGroup || list == null || form == null ||
                pending == null || effectMarker == null || pack == null)
                return;

            foreach (var key in list.GetList())
            {
                var character = list.GetItem(key).item as Character;
                if (character == null || character.group.Get() != group ||
                    !character.attributes.Contains(pending, FInt.ONE))
                    continue;

                var effect = character.GetSkillEffectOnCharacterMatchingSkillpack(pack);
                if (effect != null)
                    character.RemoveSkill(effect);

                // Older saves used base-stat changes instead of a temporary effect.
                if (!character.attributes.Contains(effectMarker, FInt.ONE))
                {
                    character.attributes.AddToBase((Tag)TAG.STRENGTH, 1f);
                    character.attributes.AddToBase((Tag)TAG.WITS, 1f);
                }

                character.UpgradeSubraceTo(form, true, false);
                character.attributes.SetBaseTo(effectMarker, FInt.ZERO);
                character.attributes.SetBaseTo(pending, FInt.ZERO);
                var change = new AdventureEventDataChange();
                change.GatherReferenceInfo(character);
                change.playerID = group.ownerID;
                change.count = 1;
                data.AddUncollectedAdventureChange(change);
            }
        }

        [ScriptAttribute(null)]
        public static bool FRA_DL_TurningReady(AdventureEventData data, LogicResultAction action, AdvNode node)
        {
            var player = GameInstance.Get().GetPlayer(data.GetMainPlayerID());
            var tag = Globals.GetInstanceFromDB<Tag>(DeadlineId);
            float deadline = player.attributes.GetBase(tag).ToFloat();
            if (deadline == 0)
                deadline = GameInstance.Get().sharedAttributes.GetBase(tag).ToFloat();
            return ServerManager.GetTurnManager().tunrIndex >= deadline;
        }
    }
}
