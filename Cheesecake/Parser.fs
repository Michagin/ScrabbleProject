module parser

open System
open FParsec
open ScrabbleUtil
//open TextParser
//open Parser

type aExp =
    | N of int              (* Integer literal *)
    | V of string           (* Variable reference *)
        
    | WL                    (* Word length *)
    | PV of aExp            (* Point value lookup at word index *)
        
    | Add of aExp * aExp    (* Addition *)
    | Sub of aExp * aExp    (* Subtraction *)
    | Mul of aExp * aExp    (* Multiplication *)
    | Div of aExp * aExp    (* Division *)
    | Mod of aExp * aExp    (* Modulo *)

    | CharToInt of cExp     (* Cast to integer *)

and cExp =
    | C  of char             (* Character literal *)
    | CV of aExp             (* Character lookup at word index *)
       
    | ToUpper of cExp        (* Convert character to upper case *)
    | ToLower of cExp        (* Convert character to lower case *)
       
    | IntToChar of aExp      (* Cast to character *)

type bExp =             
    | TT                   (* True *)
    | FF                   (* False *)
        
    | AEq of aExp * aExp   (* Numeric equality *)
    | ALt of aExp * aExp   (* Numeric less than *)
        
    | Not of bExp          (* Boolean not *)
    | Conj of bExp * bExp  (* Boolean conjunction *)
        
    | IsVowel of cExp      (* Check for vowel *)
    | IsConsonant of cExp  (* Check for constant *)

type stm =                
    | Declare of string       (* NEW: Variable declaration *)
    | Ass of string * aExp    (* variable assignment *)
    | Skip                    (* Nop *)
    | Seq of stm * stm        (* Sequential composition *)
    | ITE of bExp * stm * stm (* If-Then-Else statement *)
    | While of bExp * stm     (* While statement *)


module ImpParser =
    let (.+.) a b = Add (a, b)
    let (.-.) a b = Sub (a, b)
    let (.*.) a b = Mul (a, b)
    let (./.) a b = Div (a, b)
    let (.%.) a b = Mod (a, b)
        
    let (~~) b = Not b
    let (.&&.) b1 b2 = Conj (b1, b2)
    let (.||.) b1 b2 = ~~(~~b1 .&&. ~~b2)       (* boolean disjunction *)

    let (.=.) a b = AEq (a, b)   
    let (.<.) a b = ALt (a, b)   
    let (.<>.) a b = ~~(a .=. b)                (* numeric inequality *)
    let (.<=.) a b = a .<. b .||. ~~(a .<>. b)  (* numeric smaller than or equal to *)
    let (.>=.) a b = ~~(a .<. b)                (* numeric greater than or equal to *)
    let (.>.) a b = ~~(a .=. b) .&&. (a .>=. b) (* numeric greater than *)

    let (>*>) p1 p2 = p1 .>>. spaces .>>. p2
    let (.>*>.) p1 p2 = p1 .>> spaces .>>. p2
    let (.>*>) p1 p2 = p1 .>> spaces .>> p2
    let (>*>.) p1 p2 = p1 .>> spaces >>. p2
    let whitespaceChar = satisfy Char.IsWhiteSpace
    let pAnyChar = anyChar
    let letterChar = asciiLetter
    let alphaNumeric = asciiLetter <|> digit
    let charListToStr charList = String(List.toArray charList) |> string
    let pint = pint32
    let choice ps = ps |> Seq.map attempt |> choice
    let (<|>) p1 p2 = attempt p1 <|> attempt p2

    let pIntToChar = pstring "intToChar"
    let pPointValue = pstring "pointValue"
    let pCharToInt = pstring "charToInt"
    let pToUpper = pstring "toUpper"
    let pToLower = pstring "toLower"
    let pCharValue = pstring "charValue"
    let pTrue = pstring "true"
    let pFalse = pstring "false"
    let pif = pstring "if"
    let pthen = pstring "then"
    let pelse = pstring "else"
    let pwhile = pstring "while"
    let pdo = pstring "do"

    let delimitise char1 char2 =
        let start = pchar char1
        let ending = pchar char2
        
        fun p -> 
            start >*>. p .>*> ending        //equivalent to: start >>. spaces >>. p .>> spaces .>> ending

    let parenthesise p = 
        delimitise '(' ')' p
    
    let bracketsise p =
        delimitise '{' '}' p

    let pid =
        (letterChar .>>. many alphaNumeric) |>> fun (x,y) -> (x::y) |> List.toArray |> (fun s -> System.String s)

    let binop op = fun a b -> a .>*> op .>*>. b 

    let unop op a = op >*>. a



    let TermParse, tref = createParserForwardedToRef<aExp, unit>()
    let cParse,cref = createParserForwardedToRef<cExp, unit>()
    let ProdParse, pref = createParserForwardedToRef<aExp, unit>()
    let AtomParse, aref = createParserForwardedToRef<aExp, unit>()

    let AddParse = binop (pchar '+') ProdParse TermParse |>> Add <?> "Add"
    do tref := choice [AddParse; ProdParse]
    
    let SubParse = binop (pchar '-') ProdParse TermParse |>> Sub <?> "Sub"
    do tref := choice [AddParse; SubParse; ProdParse]

    let MulParse = binop (pchar '*') AtomParse ProdParse |>> Mul <?> "Mul"
    do pref := choice [MulParse; AtomParse]

    let DivParse = binop (pchar '/') AtomParse ProdParse |>> Div <?> "Div"
    
    let ModParse = binop (pchar '%') AtomParse ProdParse |>> Mod <?> "Mod"
    do pref := choice [MulParse; DivParse; ModParse; AtomParse]

    let NParse   = pint |>> N <?> "Int"

    let NegParse = unop (pchar '-') TermParse |>> (fun x -> Mul(N -1, x)) <?> "Neg"

    let PVParse = unop (pPointValue) (parenthesise TermParse) |>> PV <?> "PV"
    
    let CharToIntParse = unop (pCharToInt) (parenthesise cParse) |>> CharToInt <?> "CharToInt"
    
    let VParse = pid |>> V  <?> "Var"
    let ParParse = parenthesise TermParse
    do aref := choice [NegParse; NParse;  PVParse; CharToIntParse; ParParse; VParse]


    let AexpParse = TermParse 

        //Cexp
    let qoute = pstring "'"
    let CParse = (qoute >>. pAnyChar .>> qoute) |>> C <?> "C"
    let CVParse = unop (pCharValue) (parenthesise AexpParse) |>> CV <?> "CV"
    let ToUpperParse = unop (pToUpper) (parenthesise cParse) |>> ToUpper <?> "ToUpper"
    let ToLowerParse = unop (pToLower) (parenthesise cParse) |>> ToLower <?> "ToLower"
    let IntToCharParse = unop (pIntToChar) (parenthesise AexpParse) |>> IntToChar <?> "IntToChar"
    do cref := choice[CParse; CVParse; ToUpperParse; ToLowerParse; IntToCharParse; cParse]

    let CexpParse = cParse

        //Bexp
    let BTermParse,bTref = createParserForwardedToRef<bExp, unit>()
    let BProdParse, bPref = createParserForwardedToRef<bExp, unit>()
    let BAtomParse, bAref = createParserForwardedToRef<bExp, unit>()

    let ConjParse = binop (pstring @"/\") BProdParse BTermParse |>> Conj <?> "Conjuntion"
    let DisjParse = binop (pstring @"\/") BProdParse BTermParse |>> (fun x -> fst x .||. snd x) <?> "Disjuntion"
    do bTref := choice [ConjParse; DisjParse; BProdParse]

    let AEqParse = binop (pstring "=") AexpParse AexpParse |>> AEq <?> "Equality"
    let AIeParse = binop (pstring "<>") AexpParse AexpParse |>>  (fun x -> fst x .<>. snd x)  <?> "Inequality"
    let ALtParse = binop (pstring "<") AexpParse AexpParse |>> ALt <?> "Less than"
    let ALOEParse = binop (pstring "<=") AexpParse AexpParse |>> (fun x -> fst x .<=. snd x) <?> "Less Than Or Equal"
    let AGtOEParse = binop (pstring ">=") AexpParse AexpParse |>> (fun x -> fst x .>=. snd x) <?> "Less Than Or Equal"
    let AGtParse = binop (pstring ">") AexpParse AexpParse |>> (fun x -> fst x .>. snd x) <?> "Greater than"
    do bPref := choice [ALOEParse; AGtOEParse; AEqParse; AIeParse; ALtParse; AGtParse; BAtomParse]

    let BNegParse = unop (pstring "~") BTermParse |>> Not <?> "Not"
    let TrueParse = pTrue |>> (fun x ->  TT) <?> "True"
    let FalseParse = pFalse |>> (fun x -> FF) <?> "False"
    do bAref := choice [BNegParse; TrueParse; FalseParse; parenthesise BTermParse]

    let BexpParse = BTermParse

        //Stmnt
    let STermParse,sref = createParserForwardedToRef<stm, unit>()
    let SControleStmParse, scref = createParserForwardedToRef<stm, unit>()
    
    let VarParse = binop (pstring ":=") pid AexpParse |>> Ass
    let DeclareParse = pstring "declare" >>. spaces1 >>. pid |>> Declare <?> "Declare"
    let SParse = SControleStmParse .>*> (pstring ";") .>> many whitespaceChar .>*>. STermParse |>> Seq <?> "Sequential"
    let ITEParse = pif >*>. parenthesise BexpParse .>*> pthen .>*>. bracketsise STermParse .>*> pelse .>*>. bracketsise STermParse |>> (fun ((x,y),z) -> ITE(x,y,z)) <?> "IfElse"
    let IfParse = pif >*>. parenthesise BexpParse .>*> pthen .>*>. bracketsise STermParse |>> (fun (x,y) -> ITE(x,y,Skip)) <?> "If"
    let WhileParse = pwhile >*>. parenthesise BexpParse .>*> pdo .>*>. bracketsise STermParse |>> (fun (x,y) -> While(x,y)) <?> "While"
    do sref := choice[SParse; SControleStmParse]

    do scref := choice[VarParse; DeclareParse; ITEParse; IfParse; WhileParse;]


    let stmParse = STermParse

    let getParserResult s (pr : ParserResult<'a, 'b>) =
        match pr with
        | Success (t, _, _)   -> 
             t
        | Failure (err, _, _) -> 
            let errorStr = sprintf "Failed to parse %s\n\nError:\n%A" s err
            DebugPrint.debugPrint errorStr
            failwith errorStr

    let runTextParser parser text =
        match runParserOnString parser () "" text with
        | Success (stm, _, _) -> stm
        | Failure (err, _, _) -> failwith err 