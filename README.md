# Virus-Evolution

The Unity project I made to simulate the evolution of a virus on very high-level.
I explained and showcased the project in this video on my YouTube channel: [
![video_thumbnail](https://i.ytimg.com/vi/g7xzVqpyiPY/hqdefault.jpg?sqp=-oaymwEZCPYBEIoBSFXyq4qpAwsIARUAAIhCGAFwAQ==&rs=AOn4CLCqVjLzCegP8thqtZqGjj18scJ81Q)
](https://youtu.be/g7xzVqpyiPY)

## Usage
Once opened with Unity, just play the "SampleScene" scene and it should work.
Currently, most parameters are hard-coded into `Entity.cs` so you'll have to edit that if you want to tweak the simulation.  
You can use `Space` and `Shift+Space` to change the simulation to 800% and 1600% respectively (and back to 100%). 

## Data & graphs
While the simulation is running, the program saves data about the amount of infected and healthy people, average trait multipliers and entity states over time as `.csv` files inside the `CSV_DATA`folder.  
I wrote the `graph_visualizer.py` script (located inside the `VisualizationTools` folder) which is capable of displaying this data as nicely colored graphs, and if you're interested you should be able to get it running pretty easily, but it's still a work in progress and might one day be a project of it's own.