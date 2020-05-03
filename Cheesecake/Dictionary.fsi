module Dictionary
       type Dictionary 
       val internal empty    : string -> Dictionary
       val internal alphabet : Dictionary -> char list
       val internal insert   : string -> Dictionary -> Dictionary
       val internal lookup   : string -> Dictionary -> bool

