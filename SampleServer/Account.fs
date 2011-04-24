namespace SimpleServer
open MOUSE.Core
open SimpleProtocol
open SimpleProtocol.Generated


type Account() =
    interface IAccount with
        member this.Login(request) =  {IsValid = true; Account = null}
        member this.GetCharacters() = new ResizeArray<CharacterData>()

