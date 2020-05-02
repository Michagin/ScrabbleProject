namespace Cheesecake

open ScrabbleServer
open ScrabbleUtil
open ScrabbleUtil.ServerCommunication

open System.Net.Sockets
open System.IO
open DebugPrint
open Eval
open FParsec
open Parser
open Dictionary
open Blueberry
open MultiSet

module RegEx =
    open System.Text.RegularExpressions

    let (|Regex|_|) pattern input =
        let m = Regex.Match(input, pattern)
        if m.Success then Some(List.tail [ for g in m.Groups -> g.Value ])
        else None

    let parseMove ts =
        let pattern = @"([-]?[0-9]+[ ])([-]?[0-9]+[ ])([0-9]+)([A-Z]{1})([0-9]+)[ ]?" 
        Regex.Matches(ts, pattern) |>
        Seq.cast<Match> |> 
        Seq.map 
            (fun t -> 
                match t.Value with
                | Regex pattern [x; y; id; c; p] ->
                    ((x |> int, y |> int), (id |> uint32, (c |> char, p |> int)))
                | _ -> failwith "Failed (should never happen)") |>
        Seq.toList

 module Print =

    let printHand pieces hand =
        hand |>
        fold (fun _ x i -> forcePrint (sprintf "%d -> (%A, %d)\n" x (Map.find x pieces) i)) ()

module BoardState =

    type boardState = {
        boardFun      : boardFun // coord -> Map<int, squareFun> option
        origin        : coord               // center
        usedSquare    : Map<int, squareFun> // maybe ignore?
        placedTiles   : Map<int * int, tile>
    }

    type SquarePosition = 
        | UsedSquare of uint32 * char *int
        | UnusedSquare of Map<int, squareFun>
        | Hole

    let progToStm (bProg:boardProg) = Parser.ImpParser.runTextParser Parser.ImpParser.stmParse bProg.prog // stm

    let squareProgToFun (bProg:boardProg) = Map.map (fun x y -> Map.map (fun x2 y2 -> (Parser.ImpParser.runTextParser Parser.ImpParser.stmParse y2) |> stmntToSquareFun ) y) bProg.squares

    let mkBoardState (bProg:boardProg) = 1

    let insert boardSt coord tile = 1

    let query boardSt coord = 1

module State = 
    // Make sure to keep your state localised in this module. It makes your life a whole lot easier.
    // Currently, it only keeps track of your hand, and your player numer but it could, potentially, 
    // keep track of other useful
    // information, such as number of players, player turn, etc.

    type state = {
        playerNumber  : uint32
        hand          : MultiSet<uint32>
      //  boardState    : BoardState.boardState
        playMade      : bool
    }

    let mkState pn h = { playerNumber = pn; hand = h; playMade = false; }

    let newState pn hand = mkState pn hand
    
    let playerNumber st  = st.playerNumber
    let hand st          = st.hand
    let playMade st      = st.playMade

module Scrabble =
    open System.Threading

    let playGame cstream (dict:Dictionary) pieces (boardP : boardProg) (st : State.state) =

        let rec aux (st : State.state) =
            Thread.Sleep(5000) // only here to not confuse the pretty-printer. Remove later.
            Print.printHand pieces (State.hand st)
            
            // remove the force print when you move on from manual input (or when you have learnt the format)
            //let input =  System.Console.ReadLine()
            //let move = RegEx.parseMove input
            if st.playMade = true then
             debugPrint (sprintf "Player %d -> Server:\n%A\n" (State.playerNumber st) (SMPass)) // keep the debug lines. They are useful.
             send cstream (SMPass)
            else 
             let word = checkHand (st.hand) pieces |> decideWord dict
             if word = [] 
             then 
              debugPrint (sprintf "Player %d -> Server:\n%A\n" (State.playerNumber st) (SMChange (MultiSet.toList st.hand)))
              send cstream (SMChange (MultiSet.toList st.hand)) // Changes the entire hand.
             else 
              let play = List.map (fun x -> addId (Map.toList pieces) x) word
              debugPrint (sprintf "Player %d -> Server:\n%A\n" (State.playerNumber st) (SMPlay (addCoordsH (boardP.center) play)))
              send cstream (SMPlay (addCoordsH (boardP.center) play))
                                 
              
              
            let msg = recv cstream
            debugPrint (sprintf "Player %d <- Server:\n%A\n" (State.playerNumber st) msg) // keep the debug lines. They are useful.

            match msg with
            | RCM (CMPassed i) -> 
                (* your idiot of a enemy passed*)
                debugPrint "Enemy passed"
                let st' = st // This state needs to be updated
                aux st'
            | RCM (CMPlaySuccess(ms, points, newPieces)) ->
                (* Successful play by you. Update your state (remove old tiles, add the new ones, change turn, etc) *)
                let rec moveToPieces tiles acc = match tiles with
                                                    | [] -> acc 
                                                    | (c,(id,(char,p)))::xs -> moveToPieces xs (MultiSet.addSingle id acc)
                let rec newHand tiles acc = match tiles with
                                            | [] -> acc
                                            | (id,n)::xs -> newHand xs (MultiSet.add id n acc)
                let st' = {st with playMade = true; hand = newHand newPieces (subtract st.hand (moveToPieces ms MultiSet.empty)) |> toList |> ofList} // This state needs to be updated
                aux st'
            | RCM (CMChangeSuccess (newTiles)) ->
                let rec newHand tiles acc = match tiles with
                                            | [] -> acc
                                            | (id,n)::xs -> newHand xs (MultiSet.add id n acc)
                let st' = {st with hand = newHand newTiles st.hand}
                aux st'
            | RCM (CMPlayed (pid, ms, points)) ->
                (* Successful play by other player. Update your state *)
                let st' = {st with playMade = true} // This state needs to be updated
                aux st'
            | RCM (CMPlayFailed (pid, ms)) ->
                (* Failed play. Update your state *)
                let st' = st // This state needs to be updated
                aux st'
            | RCM (CMGameOver _) -> ()
            | RCM a -> failwith (sprintf "not implmented: %A" a)
            | RGPE err -> printfn "Gameplay Error:\n%A" err; aux st


        aux st

    let startGame 
            (boardP : boardProg) 
            (alphabet : string) 
            (words : string list) 
            (numPlayers : uint32) 
            (playerNumber : uint32) 
            (playerTurn  : uint32) 
            (hand : (uint32 * uint32) list)
            (tiles : Map<uint32, tile>)
            (timeout : uint32 option) 
            (cstream : Stream) =
        debugPrint 
            (sprintf "Starting game!
                      number of players = %d
                      player id = %d
                      player turn = %d
                      hand =  %A
                      timeout = %A\n\n" numPlayers playerNumber playerTurn hand timeout)
        
        let handSet = List.fold (fun acc (x, k) -> MultiSet.add x k acc) MultiSet.empty hand
        let dict = List.fold (fun acc s -> Dictionary.insert s acc) (Dictionary.empty alphabet) words
        fun () -> playGame cstream dict tiles boardP (State.newState playerNumber handSet )
        