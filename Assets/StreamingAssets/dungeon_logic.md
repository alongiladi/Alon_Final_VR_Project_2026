# Dungeon Logic Graph (obsidian.md)

 dungeon  connectivity and progression logic

## Room Connectivity Graph
- [[StartRoom]] -> [[KeyChamber]]
- [[KeyChamber]] -> [[FractalSanctuary]]
- [[FractalSanctuary]] -> [[ExitSanctuary]]

## Room Node Specifications
- [[StartRoom]]: Type=Spawn, GridX=0, GridZ=0, Torch=True, Difficulty=1
- [[KeyChamber]]: Type=KeyAltar, GridX=0, GridZ=1, Puzzle=GoldenKey, Difficulty=2
- [[FractalSanctuary]]: Type=FractalGarden, GridX=1, GridZ=1, Feature=InteractiveFractalTree, Difficulty=3
- [[ExitSanctuary]]: Type=ExitGate, GridX=2, GridZ=1, Lock=KeySocket, Difficulty=4
