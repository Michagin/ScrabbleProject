module Blueberry
open MultiSet
open ScrabbleUtil
open Dictionary
        
type word = (char * int) list

let checkHand (hand:MultiSet<uint32>) (pieces:Map<uint32,tile>) = [('H',4);('E',1);('L',1);('L',1);('O',2)]
let decideWord (dict:Dictionary) (hand:(char * int) list) = [('H',4);('E',1);('L',1);('L',1);('O',2)]

