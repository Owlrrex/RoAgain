using OwlLogging;

namespace Client
{
    public class AccountLoginPhase
    {
        public int AccountId = -1;

        public int Start()
        {
            return 0;
        }

        public bool IsStarted()
        {
            return false;
        }

        public bool IsFinished()
        {
            return false;
        }

        public int Clear()
        {
            return 0;
        }
    }

    public class CharacterLoginPhase
    {
        public LocalCharacterEntity CurrentCharacater;

        public int Start()
        {
            return 0;
        }

        public bool IsStarted()
        {
            return false;
        }

        public bool IsFinished()
        {
            return false;
        }

        public int Clear()
        {
            return 0;
        }
    }
}
