module Blueberry
open MultiSet
open ScrabbleUtil
open Dictionary
        
        val checkHand    : MultiSet<uint32> -> Map<uint32, tile> -> (char * int) list     
        val decideWord   : Dictionary -> (char * int) list -> (char * int) list  
        val addId        : (uint32 * tile) list -> (char * int) -> uint32 * (char * int)
        val addCoordsH   : coord -> (uint32 * (char * int)) list -> (coord * (uint32 * (char * int))) list
        val moveToPieces : (coord * (uint32 * (char * int))) list -> MultiSet<uint32> -> MultiSet<uint32>
        val newHand      : (uint32 * uint32) list -> MultiSet<uint32> -> MultiSet<uint32>