# About
This tool is suppose to help with choosing the easiest game to complete to bump the average game completion. 
# Important
This tool is only able to read the games you have from the steam api. 
If you're profile is private the api does not return any games.  
If you have returned a game, the api will not return that game.    
But steam will still track that game as started.  
Therefor the calculated Completion % will be of.  
```
-h --help                               Displays this info
-la --load-api      [apikey] [userid]   Loads user game data with the achievements from the api
-lf --load-file     [file]              Loads user game data from a given file
-d --dump           [file]              Dumps the user game data to a file (for -lf)
-ds --dataset       [dataset]           Outputs data                    
-hu --human                             Get Human info (e.g. total games loaded from the api)

Datasets:
c comp completion                       Prints out total completion average
l list                                  Prints out all games with achievements and the completion of that game [game]=[completion]
ls list-started                         Prints out started games (min. 1 achievement) with the completion of that game [game]=[completion]
sla sorted-list-ascending               Prints out all games with achievements and the completion of that game [game]=[completion] sorted by completion ascending
sld sorted-list-descending              Prints out all games with achievements and the completion of that game [game]=[completion] sorted by completion descending
slas sorted-list-ascending-stared       Prints out started games (min. 1 achievement) with the completion of that game [game]=[completion] sorted by completion ascending
slds sorted-list-descending-stared      Prints out started games (min. 1 achievement) with the completion of that game [game]=[completion] sorted by completion descending

ld list-unfinished                      Prints out unfinished games (min. 1 achievement) with the completion of that game [game]=[completion]
slau sorted-list-ascending-unfinished   Prints out unfinished games (min. 1 achievement) with the completion of that game [game]=[completion] sorted by completion ascending
sldu sorted-list-descending-unfinished  Prints out unfinished games (min. 1 achievement) with the completion of that game [game]=[completion] sorted by completion descending

ld list-difficulty                      Prints out unfinished games (min. 1 achievement) with the completion of that game [game]=[difficulty]
slad sorted-list-ascending-difficulty   Prints out unfinished games (min. 1 achievement) with the completion of that game [game]=[difficulty] sorted by difficulty ascending
sldd sorted-list-descending-difficulty  Prints out unfinished games (min. 1 achievement) with the completion of that game [game]=[difficulty] sorted by difficulty descending

Examples:
-la [Key] [userid] -d cache.json -ds slas
-lf cache.json -ds sldu 
```
# Also
The project name was probl. poorly chosen.