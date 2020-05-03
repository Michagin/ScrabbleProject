module Blueberry
open MultiSet
open ScrabbleUtil
open Dictionary
        
        val internal checkHand    : MultiSet<uint32> -> Map<uint32, tile> -> (char * int) list     
        val internal decideWord   : Dictionary -> (char * int) list -> (char * int) list  
        val internal addId        : (uint32 * tile) list -> (char * int) -> uint32 * (char * int)
        val internal addCoordsH   : coord -> (uint32 * (char * int)) list -> (coord * (uint32 * (char * int))) list
        val internal moveToPieces : (coord * (uint32 * (char * int))) list -> MultiSet<uint32> -> MultiSet<uint32>
        val internal newHand      : (uint32 * uint32) list -> MultiSet<uint32> -> MultiSet<uint32>