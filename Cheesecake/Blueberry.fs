module Blueberry
open ScrabbleUtil
open MultiSet
open Dictionary
        
type word = (char * int) list

let tileToTuple (t:tile) =
  match Set.toList t with
  | [c,p] -> (c,p)
  | (c,p)::xs -> ('.',p) // Wild tile
  | [] -> failwith "Blank tile given"
  
let checkHand (hand:MultiSet<uint32>) (pieces:Map<uint32,tile>) = MultiSet.toList hand |> List.map (fun x -> Map.find x pieces |> tileToTuple)

let rec onlyLetters list =
  match list with
  | [] -> []
  | (c,p)::xs -> c :: (onlyLetters xs)

// Rotations and getPerms from https://stackoverflow.com/questions/4495597/combinations-and-permutations-in-f?rq=1
let rotate lst =
    List.tail lst @ [List.head lst]

/// Gets all rotations of a list.
let getRotations lst =
    let rec getAll lst i = if i = 0 then [] else lst :: (getAll (rotate lst) (i - 1))
    getAll lst (List.length lst)

let getPerms n lst =
    let rec getPermsImpl acc n lst = seq {
        match n, lst with
        | k, x :: xs ->
            if k > 0 then
                for r in getRotations lst do
                    yield! getPermsImpl (List.head r :: acc) (k - 1) (List.tail r)
            if k >= 0 then yield! getPermsImpl acc k []
        | 0, [] -> yield acc
        | _, [] -> ()
        }
    getPermsImpl List.empty n lst |> Seq.toList

let lettersToWords (list:char list list) = (List.map (fun x -> System.String.Concat (x:char list)) list) 

let replaceFirstOcc char repchar (string:string) =
 let rec aux char repchar (charlist:char list) (acc:char list) =
   match charlist with
   | [] -> System.String.Concat (List.rev acc )
   | x::xs when x = char -> System.String.Concat (List.append (List.rev acc)(repchar::xs))
   | x::xs -> aux char repchar xs (x::acc)
 aux char repchar (Seq.toList string) []

let lookupWild (s:string) (dict:Dictionary) = 
 let rec aux (s:string) dict (alpha:char list) =
  match alpha with 
   | [] -> ""
   | x::xs when (lookup (s.Replace('.',x)) dict) -> s.Replace('.',x)
   | x::xs when (replaceFirstOcc '.' x s).Contains(".") ->
                                                            let string = aux (replaceFirstOcc '.' x s) dict (alphabet dict)
                                                            if string.Length > 0 then string else aux s dict xs
   | _::xs -> aux s dict xs
 aux s dict (alphabet dict)
 
let findValidWord (dict:Dictionary) (list: string list) =
 let rec aux list dict =
  match list with
  | [] -> ""
  | (x:string)::xs when x.Contains(".") -> if (lookupWild x dict).Length > 0 then lookupWild x dict else aux xs dict 
  | (x:string)::xs -> if (lookup x dict) then x else aux xs dict 
 aux list dict  
 
 // from Stack Overflow https://stackoverflow.com/questions/2889961/f-insert-remove-item-from-list
let rec remove i list =
     match i, list with
     | 0, x::xs -> xs
     | i, x::xs -> x::remove (i - 1) xs
     | i, [] -> failwith "index out of range"

let rec addPoints (hand:(char * int) list) i char =
 match hand with
 | [] -> (char,0),i 
 | (c,p) :: xs when c = char -> (c,p),i
 | (c,p) :: xs -> addPoints xs (i+1) char

let addPointsToList hand list =
 let rec aux hand list acc = 
  match list with
  | [] -> List.rev acc
  | x :: xs -> 
               let (cp,i) = addPoints hand 0 x
               if i > (hand.Length-1) then aux hand xs (cp :: acc) else 
               aux (remove i hand) xs (cp :: acc)
 aux hand list []

// Map.toList pieces
let rec addId pieces (cp: char * int) =
 match pieces with
 | [] -> (0u,cp)
 | (id,tile)::xs -> 
                    let (c,p) = tileToTuple tile
                    if cp = (c,p) then (id,cp) else addId xs cp

let decideWord (dict:Dictionary) (hand:(char * int) list) = 
 let rec theWord dict hand length = 
  match length with
   | 0 -> ""                             
   | x -> 
          let res = onlyLetters hand |> getPerms x |> lettersToWords |>  findValidWord dict
          if (res = "") then theWord dict hand (length-1) else res
 Seq.toList (theWord dict hand 7) |> addPointsToList hand 

let rec addCoordsH (coord: coord) (list:(uint32 * (char * int)) list) =
 let rec aux (coord: coord) (list:(uint32 * (char * int)) list) acc =
   match list,coord with 
    | [],(_,_) -> List.rev acc
    | i::xs,(x,y) -> aux (x+1,y) xs ((coord,i)::acc)
 aux coord list []

 (* Below is code used for Scrabble state logic *)
 
let rec moveToPieces (tiles:(coord * (uint32 * (char * int))) list) acc = 
                                                                        match tiles with
                                                                         | [] -> acc 
                                                                         | (c,(id,(char,p)))::xs -> moveToPieces xs (MultiSet.addSingle id acc)

let rec newHand (tiles:(uint32 * uint32) list) acc = match tiles with
                                                        | [] -> acc
                                                        | (id,n)::xs -> newHand xs (MultiSet.add id n acc)

let test = [('A',2);('O',2);('T',1);('C',2);('D',2);('.',0);('.',0)]
let testWild = [('O',2);('D',2);('V',2);('.',2);('.',2);('.',0);('.',0)]
let test2 = ['A';'V';'O';'C';'A';'D';'O']