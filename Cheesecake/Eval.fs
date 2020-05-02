module Eval

    open StateMonad

    (* Code for testing *)

    let hello = [('H',4);('E',1);('L',1);('L',1);('O',2)]
    
    let state = mkState [("x", 5); ("y", 42)] hello ["_pos_"; "_result_"]
    let emptyState = mkState [] [] []
    
    let add (a:SM<int>) (b:SM<int>) : SM<int> = a >>= fun x -> b >>= fun y -> ret (x + y)    
    let div (a:SM<int>) (b:SM<int>) : SM<int> = a >>= fun x -> b >>= fun y -> if y = 0 then fail DivisionByZero else ret (x / y)     

    type aExp =
        | N of int
        | V of string
        | WL
        | PV of aExp
        | Add of aExp * aExp
        | Sub of aExp * aExp
        | Mul of aExp * aExp
        | Div of aExp * aExp
        | Mod of aExp * aExp
        | CharToInt of cExp

    and cExp =
       | C  of char  (* Character value *)
       | CV of aExp  (* Character lookup at word index *)
       | ToUpper of cExp
       | ToLower of cExp
       | IntToChar of aExp

    type bExp =             
       | TT                   (* true *)
       | FF                   (* false *)

       | AEq of aExp * aExp   (* numeric equality *)
       | ALt of aExp * aExp   (* numeric less than *)

       | Not of bExp          (* boolean not *)
       | Conj of bExp * bExp  (* boolean conjunction *)

       | IsVowel of cExp      (* check for vowel *)
       | IsConsonant of cExp  (* check for constant *)

    let (.+.) a b = Add (a, b)
    let (.-.) a b = Sub (a, b)
    let (.*.) a b = Mul (a, b)
    let (./.) a b = Div (a, b)
    let (.%.) a b = Mod (a, b)

    let (~~) b = Not b
    let (.&&.) b1 b2 = Conj (b1, b2)
    let (.||.) b1 b2 = ~~(~~b1 .&&. ~~b2)       (* boolean disjunction *)
    let (.->.) b1 b2 = (~~b1) .||. b2           (* boolean implication *) 
       
    let (.=.) a b = AEq (a, b)   
    let (.<.) a b = ALt (a, b)   
    let (.<>.) a b = ~~(a .=. b)
    let (.<=.) a b = a .<. b .||. ~~(a .<>. b)
    let (.>=.) a b = ~~(a .<. b)                (* numeric greater than or equal to *)
    let (.>.) a b = ~~(a .=. b) .&&. (a .>=. b) (* numeric greater than *)    

    let isVowel x = 
     match System.Char.ToLower x with
      | 'a' | 'e' | 'i' |'o' | 'u' -> true
      | _ -> false

    let isConsonant x = if (System.Char.IsLetter x) && (not(isVowel x)) then true else false 

    let rec arithEval a : SM<int> = 
        match a with
        | N n -> ret n
        | V s -> lookup s
        | WL -> wordLength
        | PV a -> arithEval a >>= fun x -> pointValue x
        | Add (a,b) -> (arithEval a) >>= fun x -> (arithEval b) >>= fun y -> ret (x + y)    
        | Sub (a,b) -> (arithEval a) >>= fun x -> (arithEval b) >>= fun y -> ret (x - y)   
        | Mul (a,b) -> (arithEval a) >>= fun x -> (arithEval b) >>= fun y -> ret (x * y)   
        | Div (a,b) -> (arithEval a) >>= fun x -> (arithEval b) >>= fun y -> if y = 0 then fail DivisionByZero else ret (x / y) 
        | Mod (a,b) -> (arithEval a) >>= fun x -> (arithEval b) >>= fun y -> if y = 0 then fail DivisionByZero else ret (x % y) 
        | CharToInt c -> charEval c >>= fun x -> ret (int x)

    and charEval c : SM<char> = 
        match c with
        | C c -> ret c
        | CV a -> arithEval a >>= fun x -> characterValue x
        | ToUpper c ->  charEval c >>= fun x -> ret (System.Char.ToUpper x)
        | ToLower c -> charEval c >>= fun x -> ret (System.Char.ToLower x)
        | IntToChar a -> arithEval a >>= fun x -> ret (char x)

    let rec boolEval b : SM<bool> = 
        match b with
         | TT -> ret true
         | FF -> ret false
         | AEq (a,b) -> (arithEval a) >>= fun x -> (arithEval b) >>= fun y -> ret (x = y)  
         | ALt (a,b) -> (arithEval a) >>= fun x -> (arithEval b) >>= fun y -> ret (x < y)  
         | Not b -> boolEval b >>= fun x -> ret (not x)
         | Conj (a,b) -> (boolEval a) >>= fun x -> (boolEval b) >>= fun y -> ret (x && y) 
         | IsVowel c -> charEval c >>= fun x -> ret (isVowel x)
         | IsConsonant c -> charEval c >>= fun x -> ret (isConsonant x)

    type stm =                (* statements *)
    | Declare of string       (* variable declaration *)
    | Ass of string * aExp    (* variable assignment *)
    | Skip                    (* nop *)
    | Seq of stm * stm        (* sequential composition *)
    | ITE of bExp * stm * stm (* if-then-else statement *)
    | While of bExp * stm     (* while statement *)

    let rec stmntEval stmnt : SM<unit> = 
        match stmnt with
         | Declare s -> declare s
         | Ass (s, a) -> arithEval a >>= fun x -> update s x
         | Skip -> ret ()
         | Seq (s1, s2) -> stmntEval s1 >>>= stmntEval s2
         | ITE (b, s1, s2) -> (boolEval b >>= fun x -> if x then push >>>= stmntEval s1 >>>= pop else push >>>= stmntEval s2 >>>= pop)
         | While (b, s) -> (boolEval b >>= fun x -> if x then push >>>= stmntEval (While (b, s)) >>>= pop else ret ())

    let stmntEval3 stmnt : SM<unit> = 
      let aux cont stmnt  =
        match stmnt with
         | Declare s -> cont declare s
         | Ass (s, a) -> arithEval a >>= fun x -> update s x
         | Skip -> ret ()
         | Seq (s1, s2) -> stmntEval s1 >>>= stmntEval s2
         | ITE (b, s1, s2) -> (boolEval b >>= fun x -> if x then push >>>= stmntEval s1 >>>= pop else push >>>= stmntEval s2 >>>= pop)
         | While (b, s) -> (boolEval b >>= fun x -> if x then push >>>= stmntEval (While (b, s)) >>>= pop else ret ())
      aux id stmnt 
(* Part 3 (Optional) *)

    type StateBuilder() =

        member this.Bind(f, x)    = f >>= x
        member this.Return(x)     = ret x
        member this.ReturnFrom(x) = x
        member this.Delay(f)      = f ()
        member this.Combine(a, b) = a >>= (fun _ -> b)
        
    let prog = new StateBuilder()

    let arithEval2 a = failwith "Not implemented"
    let charEval2 c = failwith "Not implemented"
    let rec boolEval2 b = failwith "Not implemented"

    let stmntEval2 stm = failwith "Not implemented"

(* Part 4 (Optional) *) 

    type word = (char * int) list
    type squareFun = word -> int -> int -> Result<int, Error>

    let stmntToSquareFun stm : squareFun = fun w pos acc -> stmntEval stm >>>= lookup "_result_" |> evalSM (mkState [("_pos_",pos); ("_acc_",acc); ("_result_",0)] w ["_pos_"; "_acc_"; "_result_"])

    let arithSingleLetterScore = PV (V "_pos_") .+. (V "_acc_")
    let arithDoubleLetterScore = ((N 2) .*. PV (V "_pos_")) .+. (V "_acc_")
    let arithTripleLetterScore = ((N 3) .*. PV (V "_pos_")) .+. (V "_acc_")
    
    let arithDoubleWordScore = N 2 .*. V "_acc_"
    let arithTripleWordScore = N 3 .*. V "_acc_"
    
    let stmntSingleLetterScore = Ass ("_result_", arithSingleLetterScore)
    let stmntDoubleLetterScore = Ass ("_result_", arithDoubleLetterScore)
    let stmntTripleLetterScore = Ass ("_result_", arithTripleLetterScore)
    
    let stmntDoubleWordScore = Ass ("_result_", arithDoubleWordScore)
    let stmntTripleWordScore = Ass ("_result_", arithTripleWordScore)
    
    let singleLetterScore = stmntToSquareFun stmntSingleLetterScore
    let doubleLetterScore = stmntToSquareFun stmntDoubleLetterScore
    let tripleLetterScore = stmntToSquareFun stmntTripleLetterScore
    
    let doubleWordScore = stmntToSquareFun stmntDoubleWordScore
    let tripleWordScore = stmntToSquareFun stmntTripleWordScore
    
    // triggers Stack Overflows
    let oddConsonants = 
     stmntToSquareFun 
        (Seq (Declare "i",
             (Seq (Ass ("_result_", V "_acc_"),
                   While (V "i" .<. WL,
                          Seq(
                              ITE (IsConsonant (CV (V "i")),
                                   Ass ("_result_", V "_result_" .*. N -1),
                                   Skip),
                              Ass ("i", V "i" .+. N 1)))))))

    type coord = int * int

    type boardFun = coord -> Map<int, squareFun> option

    let stmntToBoardFun stm (m:Map<int,Map<int, squareFun>>) : boardFun = fun (x, y) -> stmntEval stm >>>= lookup "_result_" >>= fun id -> ret (Some (m.Item(id)))
                                                                                     |> evalSM (mkState [("_x_",x); ("_y_",y); ("_result_",0)] [] ["_x_"; "_y_"; "_result_"]) 
                                                                                     |> function
                                                                                        | Success x -> x
                                                                                        | Failure err -> failwith (sprintf "Error: %A" err)
                                                                                                                                            

    let abs v result = ITE (v .<. N 0, Ass (result, v .*. N -1), Ass (result, v))

    let twsCheck x y = ((V x .=. N 0) .&&. (V y .=. N 7)) .||.
                       ((V x .=. N 7) .&&. ((V y .=. N 7) .||. (V y .=. N 0)))

    let dwsCheck x y = (V x .=. V y) .&&. (V x .<. N 7) .&&. (V x .>. N 2)

    let tlsCheck x y = ((V x .=. N 6) .&&. (V y .=. N 2)) .||.
                       ((V x .=. N 2) .&&. ((V y .=. N 2) .||. (V y .=. N 6)))

    let dlsCheck x y = ((V x .=. N 0) .&&. (V y .=. N 4)) .||.
                       ((V x .=. N 1) .&&. ((V y .=. N 1) .||. (V y .=. N 5))) .||.
                       ((V x .=. N 4) .&&. ((V y .=. N 0) .||. (V y .=. N 7))) .||.
                       ((V x .=. N 5) .&&. (V y .=. N 1)) .||.
                       ((V x .=. N 7) .&&. (V y .=. N 4))

    let insideCheck x y = ((V x .<. N 8) .&&. (V y .<. N 8))

    let checkSquare f v els = ITE (f "xabs" "yabs", Ass ("_result_", N v), els)
    
    let standardBoardFun =
        Seq (Declare "xabs",
             Seq (Declare "yabs",
                  Seq (abs (V "_x_") "xabs",
                       Seq (abs (V "_y_") "yabs",
                            checkSquare twsCheck 4 
                                (checkSquare dwsCheck 3 
                                    (checkSquare tlsCheck 2 
                                        (checkSquare dlsCheck 1
                                            (checkSquare insideCheck 0
                                                (Ass ("_result_", N -1))))))))))

    let boardMap = [(0, singleLetterScore); (1, doubleLetterScore); (2, tripleLetterScore); 
                    (3, doubleWordScore); (4, tripleWordScore)] |> Map.ofList
 (*
    type board = {
        center        : coord
        defaultSquare : squareFun
        squares       : boardFun
    }

    let listToFunMap (m: (int * stm) list) = m|> List.map (fun (i,s) -> (i,stmntToSquareFun s)) |> Map.ofList

    let mkBoard c defaultSq boardStmnt ids : board = { center = c; defaultSquare = stmntToSquareFun defaultSq; squares = stmntToBoardFun boardStmnt (listToFunMap ids) } 

    let ids = [(0, stmntSingleLetterScore); (1, stmntDoubleLetterScore); (2, stmntTripleLetterScore); (3, stmntDoubleWordScore); (4, stmntTripleWordScore)]

    let standardBoard = 
        mkBoard (0, 0) stmntSingleLetterScore standardBoardFun ids

*)
    