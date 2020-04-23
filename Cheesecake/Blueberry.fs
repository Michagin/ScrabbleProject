module Blueberry
open MultiSet
open ScrabbleUtil
open Dictionary
        
type word = (char * int) list

let tileToTuple (t:tile) =
  match Set.toList t with
  | [c,p] -> (c,p)
  | (c,p)::xs -> ('.',p) // Wild tile
  
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

// Does not handle multiple wild tiles correctly
let lookupWild (s:string) (dict:Dictionary) = 
 let rec aux (s:string) dict (alphabet:char list) =
  match alphabet with 
   | [] -> ""
   | x::xs -> if (lookup (s.Replace('.',x)) dict) then s.Replace('.',x) else aux s dict xs
 aux s dict (alphabet dict)
 
let findValidWord (dict:Dictionary) (list: string list)  : string =
 let rec aux list dict =
  match list with
  | [] -> ""
  | (x:string)::xs when x.Contains('.') -> if (lookupWild x dict).Length > 0 then lookupWild x dict else aux xs dict 
  | (x:string)::xs -> if (lookup x dict) then x else aux xs dict 
 aux list dict  
 
let decideWord (dict:Dictionary) (hand:(char * int) list) = 
 let rec theWord dict hand length = 
  match length with
   | 0 -> ""                             
   | x -> 
          let res = onlyLetters hand |> getPerms x |> lettersToWords |>  findValidWord dict
          if (res = "") then theWord dict hand (length-1) else res
 theWord dict hand 7 
  
   
