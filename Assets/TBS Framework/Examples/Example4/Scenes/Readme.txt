This scene is based on Spann Island level from Advance Wars game series.
It was made with assets by Kenney:
- 1 bit pack - https://kenney.nl/assets/bit-pack

The level was rebuild from ground-up to showcase new features in the upcoming TBS Framework update. The changes include a new Ability System that allowed to easily implement structure-capturing mechanism and deployment of new units during gameplay. Reworked AI System made it easy to program AI players to use new abilities. Finally, custom game ending conditions were introduced - capturing enemy HQ in this case.

Instructions:

You control the blue team. Recruit units in factories, use scouts to capture more bases and occupy enemy HQ to win.

There are three types of units in the game:

Name: Scout

Price: 1000

Description: Scout is the cheapest unit with low firepower. It is the only unit that can capture structures



Name: Knight

Price: 7000

Description: Knight an expensive, but powerfull unit with low mobility



Name: Wizard

Price: 6000

Description: Wizard is a moderately-priced unit with a strong ranged attack and weaker meele attack



Units will deal damage proportionally to their remaining health.



There are three kinds of structures in the game:

-City 

-Factory - can spawn additional units

-HQ - capture enemy HQ to win the game

Each structure generates 500 gold  each turn for the player that controls it. Use scout to capture neutral and enemy structures by dropping it's loyality stat to 0. Remaining loyality is indicated by number in the upper-left corner of the structure.



The game incoroporates terrain system, where each terrain type has different movement cost and defence bonus. 

+--------+---------------+---------------+
|        | Movement Cost | Defence Boost |
+--------+---------------+---------------+
| Road   |          0.75 |             0 |
| Plains |             1 |             0 |
| City   |             1 |             2 |
| Tower  |             1 |             3 |
| Castle |             1 |             4 |
| Forest |          1.25 |             2 |
| Rough  |           1.5 |             0 |
+--------+---------------+---------------+
