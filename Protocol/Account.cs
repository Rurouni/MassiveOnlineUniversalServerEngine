using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using MOUSE.Core;

namespace Protocol
{
    [NodeEntityContract]
    public interface IAccountOperations
    {
        [NodeEntityOperation(Target = OperationTargetType.Any, AdditionalResultCode = typeof(AccountLoginFailedReason))]
        AccountData Login(string name, string password);

        [NodeEntityOperation(AdditionalResultCode = typeof(CharacterCreationFailedReason))]
        CharacterData CreateCharacter(string name);

        void DeleteCharacter(long characterId);

        void SelectCharacter(long characterId);

        void Logout();
    }

    public enum AccountLoginFailedReason
    {
        IncorrectNameOrPassword,
        Banned
    }

    public enum CharacterCreationFailedReason
    {
        IncorrectNameOrPassword,
        Banned
    }


    public class AccountData
    {
        public long Id;
        public string Name;
        public List<CharacterData> Characters;
    }

    public class CharacterData
    {
        public long Id;
        public string Name;
    }
}
