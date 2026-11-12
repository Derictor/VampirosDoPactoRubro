using DBDef;
using Thea2.Client;
using Thea2.Common;
using Thea2.Server;

namespace GameScript
{
    public class DL_Coop : ScriptBase
    {
        private const string CardId = "TRAIT_CARD-DL_NIGHT_SPAWN";
        private const string ModuleName = "DL_AForjaRubra";
        private const string DeadlineId = "TAG-DL_TURNING_DEADLINE";

        public static string UIS_DL_RecruitAndUnlock(object unused)
        {
            UnlockForCurrentPlayer(1, 3);
            return "Receber Derictor no grupo.";
        }

        public static string UIS_DL_OfficeAndUnlock(object unused)
        {
            UnlockForCurrentPlayer(2, 2);
            return "Deixar a forja para depois.";
        }

        private static void UnlockForCurrentPlayer(int eventId, int nodeId)
        {
            var profile = PlayerProfile.selectedProfile;
            if (profile == null || FSMCoreGame.Get() == null)
                return;

            var waiting = FSMCoreGame.GetAdventuresWaiting();
            if (waiting == null || waiting.Count == 0)
                return;

            var adventure = waiting[0];
            var visited = adventure.visitedNodes;
            if (adventure.mainPlayerID != NetworkManager.GetLocalPlayerID()
                || adventure.moduleName != ModuleName || adventure.advIndex != eventId
                || visited == null || visited.Count == 0 || visited[visited.Count - 1] != nodeId)
                return;

            var card = Globals.GetInstanceFromDB<Trait>(CardId);
            if (card != null && profile.Unlock(card))
                PlayerProfile.Save();
        }

        [ScriptAttribute(null)]
        public static void FMO_DL_StartTurning(NOAdventureStatus status, AdventureEventData data,
            LogicModifier modifier, AdvNode node)
        {
            var player = GameInstance.Get().GetPlayer(data.GetMainPlayerID());
            var tag = Globals.GetInstanceFromDB<Tag>(DeadlineId);
            player.attributes.SetBaseTo(tag, (float)ServerManager.GetTurnManager().tunrIndex + 5);
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
