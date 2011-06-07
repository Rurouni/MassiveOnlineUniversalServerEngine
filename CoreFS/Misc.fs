namespace MOUSE.Core
open System.Collections.Generic

[<AutoOpen>]
module public DictionaryExtensions =
  
  let tryFind (d:Dictionary<'K,'T>) key =
    let (found, retVal) = d.TryGetValue key 
    if found then Some retVal
    else None
  let toSeq d = d |> Seq.map (fun (KeyValue(k,v)) -> (k,v))
  let toArray (d:IDictionary<_,_>) = d |> toSeq |> Seq.toArray
  let toList (d:IDictionary<_,_>) = d |> toSeq |> Seq.toList

  type System.Collections.Generic.Dictionary<'K,'T> with
    member this.TryFind key =  tryFind this key


