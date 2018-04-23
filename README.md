# GitAnalysisTestStuff

The idea was to track changes in the **A**bstract **S**yntax **T**ree (AST) of a project over its commits and figure out what happend in each commit (in a more precice way than added X lines, removed Y).
Works as a prototype, but has problems handling decent sized repositories. 

Roughly working
	- simple AST transitions
	- triming the graph to remove *NoCodeChange* AST Transitions
	- Gather statistics how often functions where changed
	- some simple visualization as a graph and code

**Abandoned** 
It is still small enough to rewrite and salvage for parts instead of starting to refactor the how thing. 

