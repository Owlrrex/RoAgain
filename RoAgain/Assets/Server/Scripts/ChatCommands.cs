using OwlLogging;

namespace Server
{
    public class HealChatCommand : AChatCommand
    {
        // Args[1]: [Optional] Heal mode - 0 = both, 1 = hp, 2 = sp, default = 0
        // Args[2]: [Optional] Name of character to heal, default = sender
        public override int Execute(CharacterRuntimeData sender, string[] args)
        {
            if(args.Length > 3)
            {
                OwlLogger.LogError($"Too many arguments for HealChatCommand - 2 expected, {args.Length} given!", GameComponent.ChatCommands);
                return -1;
            }

            int healMode = 0;
            string targetName = sender.Name;

            if(args.Length >= 2)
            {
                if (!int.TryParse(args[1], out healMode))
                {
                    return -2;
                }

                if(args.Length == 3)
                {
                    targetName = args[2];
                }
            }

            CharacterRuntimeData target = sender;
            if(targetName != sender.Name)
            {
                foreach (CharacterRuntimeData charData in ServerMain.Instance.Server.LoggedInCharacters)
                {
                    if (charData.Name == targetName)
                    {
                        target = charData;
                        break;
                    }
                        
                }
                if (target == sender)
                {
                    OwlLogger.Log($"Tried to use HealChatCommand on target {targetName}, which wasn't found.", GameComponent.ChatCommands);
                    return -3;
                }
            }

            ServerMapInstance targetMap = ServerMain.Instance.Server.MapModule.GetMapInstance(target.MapId);
            if(targetMap == null)
            {
                OwlLogger.LogError($"Couldn't find target map {target.MapId}!", GameComponent.ChatCommands);
                return -4;
            }
            BattleModule battleModule = targetMap.BattleModule;

            switch(healMode)
            {
                case 0:
                    battleModule.ChangeHp(target, target.MaxHp.Total, sender);
                    battleModule.ChangeSp(target, target.MaxSp.Total);
                    break;
                case 1:
                    battleModule.ChangeHp(target, target.MaxHp.Total, sender);
                    break;
                case 2:
                    battleModule.ChangeSp(target, target.MaxSp.Total);
                    break;
                default:
                    OwlLogger.LogError($"Invalid heal mode {healMode}!", GameComponent.ChatCommands);
                    return -5;
            }

            return 0;
        }
    }

    public class ChangeJobChatCommand : AChatCommand
    {
        public override int Execute(CharacterRuntimeData sender, string[] args)
        {
            return 0;
        }
    }

    public abstract class AChatCommand
    {
        /// <summary>
        /// Executes a chat command's logic
        /// </summary>
        /// <param name="args">{0] = command word, after that the command's arguments</param>
        /// <returns>0 = success</returns>
        public abstract int Execute(CharacterRuntimeData sender, string[] args);
    }
}
