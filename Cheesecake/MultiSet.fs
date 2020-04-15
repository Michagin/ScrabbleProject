module MultiSet 
    let removeLastChars (str:string) = str.Remove(str.Length-2)
    let insertStringAtEnd (s:string) (str:string) = str.Insert(str.Length,s)
    [<StructuredFormatDisplay("{AsString}")>]
    type MultiSet<'a when 'a : comparison> =
    | MS of Map<'a,uint32> 
      override this.ToString() = 
       match this with
       | MS map -> map |> Map.toList |> (List.fold (fun acc (k,v) -> acc + ("(" + (k.ToString() + ", " + "#" + (string (int v)) + "), "))) "{") |> removeLastChars |> insertStringAtEnd "}"
      member this.AsString = this.ToString()                            
    let empty = MS (Map.empty)
    let size (MS s) = Map.fold (fun a _ (t:uint32) -> a + t) 0u s
    let isEmpty (MS s) = size (MS s) = 0u
    let contains a (MS s) = s.ContainsKey(a) 
    let numItems a (MS s) = if s.ContainsKey(a) then uint32 (s.Item(a)) else 0u
    let add a n (MS s) = MS (s.Add (a, (n + if s.ContainsKey(a) then (s.Item(a)) else 0u)))
    let addSingle a (MS s) = add a 1u (MS s)
    let remove a n (MS s) = if s.ContainsKey(a) then (MS (if s.Item(a) > n then (s.Add (a, (s.Item(a) - n))) else (s.Remove (a)))) else MS s
    let removeSingle a (MS s) = remove a 1u (MS s)
    let fold f acc (MS s) = Map.fold f acc s
    let foldBack f (MS s) acc = Map.foldBack f s acc
    let ofList (list:list<'a>) = MS (Map.ofList (list |>  Seq.countBy id |> Seq.toList |> List.map (fun (k,v) -> (k,uint32 v))))
    let toList (MS s) = seq {for KeyValue (k,v) in s do for _ in 1 .. int v do yield k} |> Seq.toList
    let map f (MS s) =  (MS s) |> toList |> List.map f |> ofList
    let union (MS s1) (MS s2) = MS (Map.ofSeq( (Map.toSeq s2 |> Seq.append (seq {for KeyValue (k,v) in s1 do if (s2.ContainsKey(k) && s2.Item(k) > s1.Item(k)) || (s2.ContainsKey(k) = false) then yield (k,v)}))))
    let sum (MS s1) (MS s2) = (MS s1) |> toList |> List.append (toList (MS s2)) |> ofList
    let subtract (MS s1) (MS s2) = MS (Map.ofSeq( (Seq.append (Map.toSeq s1) (seq {for KeyValue (k,v) in s2 do if (s1.ContainsKey(k)) && (s1.Item(k) >= s2.Item(k)) then yield (k,(s1.Item(k)-v)) else yield (k,0u)}))))
    let intersection (MS s1) (MS s2) = MS (Map.ofSeq( seq {for KeyValue (k,v) in s2 do if (s1.ContainsKey(k)) then yield (k,v)}))

    // We are aware union and subtract are not the prettiest one-liners, and that
    // abusing the order Seq.append appends in for the Map.ofSeq is a bit sketchy. 