module Dictionary
      type Dictionary =
      | Node of Map<char,(Dictionary * bool)>
      | Leaf 
      
      let empty (s:string) = Node( Map.empty |> Map.toSeq |> Seq.append (seq {for c in s.ToCharArray() do yield (c,(Leaf,false))}) |> Map.ofSeq)

      let alphabet (dict:Dictionary) = 
       match dict with 
       | Node m -> m |> Map.toSeq |> Seq.map fst |> Seq.toList // requires "Dictionary.empty alphabet" is done in startGame
       | Leaf -> failwith "Requires alphabet has been loaded into dictionary"

      let rec insert (s:string) (dict:Dictionary) = 
       match dict with
       | dict when s.Length = 0 -> dict
       | Leaf when s.Length = 1 -> Node (Map.empty.Add((s.[0]), (Leaf, true)))
       | Leaf -> Node (Map.empty.Add(((s.[0]), ((insert (s.Substring(1)) Leaf), false))))
       | Node m when s.Length = 1 && m.ContainsKey(s.[0]) -> 
                                                                let (d, _) = m.Item(s.[0])
                                                                Node(m.Add((s.[0]), (d, true)))
       | Node m when s.Length = 1 -> Node(m.Add(s.[0], (Leaf,true)))
       | Node m when m.ContainsKey(s.[0]) -> 
                                                          let (d, b) = m.Item(s.[0])
                                                          Node(m.Add((s.[0]), (insert (s.Substring(1)) d, b)))                                          
       | Node m -> Node(m.Add(((s.[0]), ((insert (s.Substring(1)) Leaf), false))))
                 
      let rec lookup (s:string) (dict:Dictionary) =
       match dict with
       | Leaf -> false
       | Node m when not (m.ContainsKey(s.[0])) -> false
       | Node m when s.Length = 1  -> 
                                     let (_, b) = m.Item(s.[0])
                                     b
       | Node m -> 
                  let (d, _) = m.Item(s.[0])
                  lookup (s.Substring(1)) d

// Couldn't figure out why Test 202 fails.