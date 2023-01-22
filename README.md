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

Datasets:
c       Print total completion
u       Unfinished
a       Started
y       Print completion
x       Print difficulty
g       Sorted by completion ASC
h       Sorted by completion DESC
i       Sorted by difficulty ASC
j       Sorted by difficulty DESC
Examples:
-la [Key] [userid] -d cache.json -ds c      This will print out the total completion average.
-lf cache.json -ds auix                     This will print out a all games and there difficulty to 100% sorted by the difficulty.
```
# Also
The project name was probl. poorly chosen.