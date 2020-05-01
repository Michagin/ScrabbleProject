module StateMonad

    type Error = 
        | VarExists of string
        | VarNotFound of string
        | IndexOutOfBounds of int
        | DivisionByZero 
        | ReservedName of string           

    type Result<'a, 'b>  =
        | Success of 'a
        | Failure of 'b

    type State = { vars     : Map<string, int> list
                   word     : (char * int) list 
                   reserved : Set<string> }

    type SM<'a> = S of (State -> Result<'a * State, Error>)

    let removeFirst lst =
     match lst with
     | [] -> []
     | _::xs -> xs

    let rec remove i l =
        match i, l with
        | 0, x::xs -> xs
        | i, x::xs -> x::remove (i - 1) xs
        | i, [] -> failwith "index out of range"

    let rec insert v i l =
        match i, l with
        | 0, xs -> v::xs
        | i, x::xs -> x::insert v (i - 1) xs
        | i, [] -> failwith "index out of range"

    let rec replaceFirst lst var value =
     match lst with
     | [] -> []
     | x::xs when x = var -> value :: xs
     | _::xs -> replaceFirst xs var value

    let mkState lst word reserved = 
           { vars = [Map.ofList lst];
             word = word;
             reserved = Set.ofList reserved }

    let evalSM (s : State) (S a : SM<'a>) : Result<'a, Error> =
        match a s with
        | Success (result, _) -> Success result
        | Failure error -> Failure error

    let bind (f : 'a -> SM<'b>) (S a : SM<'a>) : SM<'b> =
        S (fun s ->
              match a s with
              | Success (b, s') -> 
                match f b with 
                | S g -> g s'
              | Failure err     -> Failure err)


    let ret (v : 'a) : SM<'a> = S (fun s -> Success (v, s))
    let fail err     : SM<'a> = S (fun s -> Failure err)

    let (>>=)  x f = bind f x
    let (>>>=) x f = x >>= (fun () -> f)

    let push : SM<unit> = 
        S (fun s -> Success ((), {s with vars = Map.empty :: s.vars}))

    let pop : SM<unit> = 
        S (fun s -> Success ((), {s with vars = removeFirst s.vars}))

    let wordLength : SM<int> = 
         S (fun s -> Success (s.word.Length, s))

    let characterValue (pos : int) : SM<char> = 
        S (fun s -> 
          if s.word.Length > pos then
           let char,_ = s.word.Item(pos)
           Success (char,s)
          else
           Failure(IndexOutOfBounds pos))

    let pointValue (pos : int) : SM<int> = 
        S (fun s -> 
                 if s.word.Length > pos && pos > -1 then
                  let _,p = s.word.Item(pos)
                  Success (p,s)
                 else
                  Failure(IndexOutOfBounds pos))

    let lookup (x : string) : SM<int> = 
        let rec aux =
            function
            | []      -> None
            | m :: ms -> 
                match Map.tryFind x m with
                | Some v -> Some v
                | None   -> aux ms

        S (fun s -> 
              match aux (s.vars) with
              | Some v -> Success (v, s)
              | None   -> Failure (VarNotFound x))

    let declare (var : string) : SM<unit> =
        S (fun s -> 
                if s.reserved.Contains(var) then Failure (ReservedName var)
                else match s.vars with
                     | x::_ when x.ContainsKey(var) -> Failure (VarExists var)
                     | _ -> Success ((), {s with vars = Map.ofList [(var,0)] :: s.vars}))

    let update (var : string) (value : int) : SM<unit> = 
            let rec aux i =
                function
                | []      -> -1
                | m :: ms -> 
                    match Map.tryFind var m with
                    | Some _ -> i
                    | None   -> aux (i+1) ms 

            S (fun s -> 
                  match aux 0 (s.vars) with
                  | -1   -> Failure (VarNotFound var)
                  | x -> Success ((), {s with vars = insert ((List.item(x) s.vars).Add (var, value)) x (remove x s.vars)}))
                        

    