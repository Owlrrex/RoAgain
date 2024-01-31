using OwlLogging;

namespace Server
{
    public abstract class AChatCommand
    {
        /// <summary>
        /// Executes a chat command's logic
        /// </summary>
        /// <param name="args">{0] = command word, after that the command's arguments</param>
        /// <returns>0 = success</returns>
        public abstract int Execute(string[] args);
    }

    public class ChangeJobChatCommand : AChatCommand
    {
        public override int Execute(string[] args)
        {
            return 0;
        }
    }
}
