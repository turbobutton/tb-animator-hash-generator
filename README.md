# TB Animator Hash Generator
A Unity tool that auto-generates hashed versions of the Animator Controller parameters in your project.

# The Problem
Since Animator Controller parameters are string-based, it's far too easy to introduce typos into your code. It can potentially cost hours of bug hunting when an animation isn't playing the way it should and you FINALLY discover that the string in your code doesn't match the name of the parameter. We've all been there...

# The Solution
Which brings us to the Animator Hash Generator. This tool scrapes the parameters from the given Animator Controllers and generates a script that caches all the strings into convenient, type-safe variables. Additionally, the tool uses Animator.StringToHash() to store the values as integers to help squeeze a little extra performance out of your animation code!

![The basic tool setup](/Images/AnimatorHashGenerator_01.png?raw=true)