# About
This tool is suppose to help with choosing the easiest game to complete to bump the average game completion. 
# Important
This tool is only able to read the games you have from the steam api. 
If you're profile is private the api does not return any games.  
If you have returned a game, the api will not return that game.    
But steam will still track that game as started.  
Therefor the calculated Completion % will be of.  
```
Args
-----
-h --help                               Displays this info
-la --load-api      [apikey] [userid]   Loads user game data with the achievements from the api
-lf --load-file     [file]              Loads user game data from a given file
-le --load-external [file]              Loads external file to add to the library. This is for unlisted games.
-d --dump           [file]              Dumps the user game data to a file (for -lf)
-ds --dataset       [dataset]           Outputs data                       

Datasets:

Print output with
c       Print total completion
r       Print total game count
y       Print completion
x       Print difficulty
z       Print total achievement count

sort // limit the dataset with
------------------------------
u       Only Unfinished (remove all finished (100%))
a       Only Started (remove all not started (0%))
g       Sorted by completion ASC
h       Sorted by completion DESC
i       Sorted by difficulty ASC
j       Sorted by difficulty DESC

Examples:
-la [Key] [userid] -d cache.json -ds c      This will print out the total completion average.
-lf cache.json -ds auix                     This will print out a all games and there difficulty to 100% sorted by the difficulty.
```
# Adding other games to the calculation
If you want to manually add games to the calculation, create a extra file with the following content:
```json
[
  {
    "Id":1205550,
    "Name":"New World PTR",
    "Achievements":133,
    "Unlocked":52
  },
  {
    "Id":1625450,
    "Name":"Muck",
    "Achievements":49,
    "Unlocked":16
  }
]
```
You can add as many games as you want.
Use `--load-external` to load this type of file!
# Also
The project name was probl. poorly chosen.