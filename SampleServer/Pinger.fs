namespace SampleDomain

open MOUSE.Core
open SampleDomain.Interfaces


type Pinger() = 
    inherit BaseEntity()

    interface IPinger with
        member this.Ping(requestId) = 
            async{
                return requestId
            }
        

