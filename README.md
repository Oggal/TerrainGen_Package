# Oggal's Terrain Gen - for Unity Package Manager
Oggal's Terrain Gen for Unity has been under development by a solo-dev since 2017. 
Over this time the project has been restarted, rebuilt, and redesigned over and over.
In 2022 the project was made into a unity package for ease of development and installation.
Originally designed as an experient to better understand mesh generation and Perlin noise, 
the package generates mesh objects for visuals and physics collisons.

### Features
Over the course of development I've tried to keep the generator easy to expand and customize to a project's needs. Some features include:
<li> Generate seperate Visual and Collision meshes
<li> Provide system for spawning and placement of terrain decoration objects such as rocks, cliffs, trees, etc.
<li> Provide system for Point Of Interest palcement and generation
<li> Allow for endless generation of terrain across X/Z plane 
<li> Provide abstract classes for extending ability to control terrain heights  with noise object creation
<li> Provide abstract classes for extending ability to control POI object generation

### Limitations
When selecting the right tool for a job I find it important to understand what a tool is not designed to do.
To better implement key features of this package certain design choses have been made that limit what this tool can do. Some key limitations are:<list>
<li>Terrains are mesh component game objects rather than unity's native terrain objects
<li>Terrain Objects are not designed to be modified after generation
<li>POI Objects only generate in a radius around terrain center
<li>Terrain Decoration Objects currently support only one mesh per decor type
</list>

## Installation
This package is designed to be installed through unity's package manager via git url.
 <li> Open your Unity project.
 <li> Open the Unity Package Manager from <b>Window > Package Manager</b>
 <li> Select the '+' menu from the top left of the package manager and select <b>"add package from git url"</b>
 <li> Paste the git URL for this repo into the text box.
 <li> Congrats! You've installed my terrain generator!

## Usage
Creation of terrain meshes in editor or runtime is designed to be simple, and quick to get started!
Simply add an empty game object and give it a <b>World Gen</b> Component!
Further functionality can be added through the <b>POI Gen</b> Component.

## Issues
This project is still under development, many issues are currently tracked through my personal notes. If any issues are discovered through use feel free to report them here on github! Issues reported to github will take priority over my notes, to ensure this can be a usefull tool for anyone interested!

## Contact
If you have any questions or concerns feel free to reach out to me <a href="mailto:philip.dan.taylor+TerrainGen@gmail.com">via email!</a>

Continued develpment of this project is fueled mostly by my own curiosity and desire to learn. If you find this tool useful, interesting, or just want to learn more, feel free to connect with me on <a href="https://www.linkedin.com/in/philiptaylor-oggal/">LinkedIn!</a>

Want to see more of my work? Check out my <a href="https://oggal.itch.io/">Itch.io page</a> to see my game projects,
and keep an eye out for projects featruing this terrain generator!