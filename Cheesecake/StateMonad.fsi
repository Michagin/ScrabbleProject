module StateMonad

    type internal SM<'a>
    type internal State

    type internal Result<'a, 'b>  =
        | Success of 'a
        | Failure of 'b

    type internal Error = 
        | VarExists of string
        | VarNotFound of string
        | IndexOutOfBounds of int
        | DivisionByZero 
        | ReservedName of string

    val internal mkState : (string * int) list -> (char * int) list -> string list -> State

    val internal ret    : 'a -> SM<'a>
    val internal fail   : Error -> SM<'a>
    val internal (>>=)  : SM<'a> -> ('a -> SM<'b>) -> SM<'b>
    val internal (>>>=) : SM<unit> -> SM<'a> -> SM<'a>

    val internal evalSM : State -> SM<'a>  -> Result<'a, Error>

    val internal push : SM<unit>
    val internal pop  : SM<unit>

    val internal lookup  : string -> SM<int>
    val internal update  : string -> int -> SM<unit>
    val internal declare : string -> SM<unit>

    val internal wordLength     : SM<int>
    val internal characterValue : int -> SM<char>
    val internal pointValue     : int -> SM<int>