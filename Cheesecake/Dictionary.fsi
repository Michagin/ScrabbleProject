module Dictionary
       type Dictionary 
       val empty    : string -> Dictionary
       val alphabet : Dictionary -> char list
       val insert   : string -> Dictionary -> Dictionary
       val lookup   : string -> Dictionary -> bool

