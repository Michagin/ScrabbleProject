module Blueberry
open MultiSet
open ScrabbleUtil
open Dictionary
        
        val checkHand  : MultiSet<uint32> -> Map<uint32, tile> -> (char * int) list     
        val decideWord : Dictionary -> (char * int) list -> string
