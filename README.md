# TB Animator Hash Generator
A Unity tool that auto-generates hashed versions of the Animator Controller parameters in your project.

# The Problem
Since Animator Controller parameters are string-based, it's far too easy to introduce typos into your code. It can potentially cost hours of bug hunting when an animation isn't playing the way it should, only to discover that the string in your code doesn't match the name of the Animator Controller parameter. We've all been there...

# The Solution
Which brings us to the Animator Hash Generator. This tool scrapes the parameters from the given Animator Controllers and generates a script that caches all the strings into convenient, type-safe variables. Additionally, the tool uses Animator.StringToHash() to store the values as integers to help squeeze a little extra performance out of your animation code.

# How To
Go to Tools > Generate Animator Hashes...

![Toolbar location](/Images/AHG_Instructions_01.png?raw=true)

A window will open that looks something like this:

![The tool with its sections labeled](/Images/AHG_Instructions_01.png?raw=true)

1. In the left hand column is a list of presets. You can add or remove presets as needed. This allows you to easily create different settings and save locations for different groups of Animator Controllers in your project.

2. This section lets you define which Animator Controllers you want to include in your generated file. There are two options:
	### Folder
	This option allows you to choose a folder that contains all the Animator Controllers you want to include in your file. All sub folders will also be included in the search.
	![The tool with its sections labeled](/Images/AHG_Instructions_ControllersCloseup_01.png?raw=true)
	### Controllers List
	This option allows you to define a list of specific Animator Controllers you want to include. You can multi-select them from your project view and drop them in the box to add them to the list.
	![The tool with its sections labeled](/Images/AHG_Instructions_ControllersCloseup_02.png?raw=true)

3. The "Saving" section lets you choose where your generated file will be stored in your project.