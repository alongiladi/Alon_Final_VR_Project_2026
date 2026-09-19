# dungeon logic graph obsidian.md

 dungeon  connectivity and progression logic

## room connectivity graph
- [[StartRoom]] -> [[KeyChamber]]
- [[KeyChamber]] -> [[FractalSanctuary]]
- [[FractalSanctuary]] -> [[ExitSanctuary]]

## room nodes + attributes + location onmaze grid 
- [[StartRoom]]: Type=Spawn, GridX=0, GridZ=0, Torch=True, Difficulty=1
- [[KeyChamber]]: Type=KeyAltar, GridX=0, GridZ=1, Puzzle=GoldenKey, Difficulty=2
- [[FractalSanctuary]]: Type=FractalGarden, GridX=1, GridZ=1, Feature=InteractiveFractalTree, Difficulty=3
- [[ExitSanctuary]]: Type=ExitGate, GridX=2, GridZ=1, Lock=KeySocket, Difficulty=4
