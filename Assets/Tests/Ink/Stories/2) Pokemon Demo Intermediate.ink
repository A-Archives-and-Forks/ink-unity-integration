// -> is a "divert" and means "go to this part of the story!"
-> Professor_Oaks_Lab

// === is a "knot".
// Knots can be "diverted 
=== Professor_Oaks_Lab
When I was young, I was a serious juggler!
In my old age, I have only 3 left, but you can have one!

// -> means "go to"
-> choose_pokemon

// a single = is a "stitch". Stitches are sub-content inside a knot.
= choose_pokemon

// This is called a Shuffle. Each time you revisit this line, the text will change!
{~Choose one!|Which Pokemon would you like?|You can pick whichever you'd like!}

// + means "make this a choice!"
+ Charmander?
    So, you want the fire Pokemon, Charmander?
    // Choices can be nested inside a choice by adding a second * mark.
    + + Yeah!
        -> picked_charmander
    + + Na.
        -> choose_pokemon
+ Squirtle?
    So, you want the water Pokemon, Squirtle?
    + + Yeah!
        -> picked_squirtle
    + + Na.
        -> choose_pokemon
+ Bulbasaur?
    So, you want the plant Pokemon, Bulbasaur?
    + + Yeah!
        -> picked_bulbasaur
    + + Na.
        -> choose_pokemon
// * is also a choice, except once chosen it's removed from the story.
* Pikachu!
    I don't have a Pikachu to give you! You'll have to catch your own!
    -> choose_pokemon

// 
= picked_charmander
You pick the Pokeball containing Charmander
-> rival_picks_pokemon

= picked_squirtle
You pick the Pokeball containing Squirtle
-> rival_picks_pokemon

= picked_bulbasaur
You pick the Pokeball containing Bulbasaur
-> rival_picks_pokemon


= rival_picks_pokemon
Your rival walks over and picks the Pokeball containing

{
	- picked_charmander:
	    // <> is called "glue". It joins the text after it to end of the last line.
	    <> Squirtle.
	- picked_squirtle:
	    <> Bulbasaur.
	- else:
		<> Charmander.
}
- Great! You're ready to start your adventure!

// Done tells the story that it's finished
-> DONE


