# TB Animator Hash Generator
A Unity tool that auto-generates files containing cached, type-safe variables for the Animator Controller parameters in your project. Basically, it prevents you from having to write this sort of boilerplate over and over.

![Boilerplate Example](/Images/AHG_Boilerplate_Example_01.png?raw=true)

## The Problem
Since Animator Controller parameters are string-based, it's really easy to introduce typos into your code. It can cost hours of bug hunting when an animation isn't playing the way it should, only to discover that the string in your code doesn't match the name of the Animator Controller parameter. We've all been there...

## The Solution
Which brings us to the Animator Hash Generator. This tool scrapes the parameters from the given Animator Controllers and generates a script that caches all the strings into convenient, type-safe variables. Additionally, the tool uses Animator.StringToHash() to store the values as integers to help squeeze a little extra performance out of your animation code.

# How To
To open the Animator Hash Generator, go to Tools > Generate Animator Hashes...

![Toolbar location](/Images/AHG_Instructions_01.png?raw=true)

A window will open that looks like this:

![Overview image of the tool](/Images/AHG_Instructions_02.png?raw=true)

## Settings Section

### Animator Controllers
The section on the right under the "Settings" tab lets you define which Animator Controllers you want to include in your generated file. There are two options:
#### "Folder"
This option allows you to choose a folder that contains all the Animator Controllers you want to include in your file. All sub folders will also be included in the search.

![Closeup of folder section](/Images/AHG_Instructions_AnimatorControllers_Folder_01.png?raw=true)
	
#### "Controllers List"
This option allows you to define a list of specific Animator Controllers you want to include. You can multi-select them from your project view and drop them in the box to add them to the list.
	
![Closeup of Animator Controllers section](/Images/AHG_Instructions_AnimatorControllers_List_01.png?raw=true)

### Saving
The "Saving" section lets you choose where your generated file will be stored in your project.

![Closeup of saving section](/Images/AHG_Instructions_Saving_01.png?raw=true)

## Formatting Section
The Formatting section contains various options for controlling the formatting of the generated variable names. You can mess around with these settings until you find something that suits your preferences. You can preview how these settings affect the variable names in the help boxes.

**NOTE:** In order for the formatting to be properly applied to the variable names, it is assumed that your Animator Controller properties use camel case and your layers use spaces (following the formatting of the built-in "Base Layer").

![Formatting tab](/Images/AHG_Instructions_Formatting_01.png?raw=true)

## Presets
In the left hand column is a list of presets. You can add or remove presets as needed. This allows you to easily create different settings and save locations for different groups of Animator Controllers in your project.

![Closeup of presets section](/Images/AHG_Instructions_Presets_01.png?raw=true)

# Using the Code
Once you generate a file, the class will look something like this (depending on your formatting settings):
~~~~
public static class AnimHashIDs
{
	//TestController_01
	public static readonly int WALK_TRIGGER = Animator.StringToHash ("walk");
	public static readonly int IS_WALKING_BOOL = Animator.StringToHash ("isWalking");
	public static readonly int WALK_SPEED_FLOAT = Animator.StringToHash ("walkSpeed");
	public static readonly int WALK_VARIATION_INT = Animator.StringToHash ("walkVariation");
	
	public static class Layers
	{
		//TestController_01
		public static readonly string BASE_LAYER = "Base Layer";
		public static readonly string LEGS = "Legs";
		public static readonly string TORSO = "Torso";
	}
}
~~~~

Then in your animation code you can reference these variables easily like so:
~~~~
public class MyAnimationClass : MonoBehaviour
{
	[SerializeField]
	private Animator _Animator;
	
	private int _legsLayerID;
	
	void Awake ()
	{
		_legsLayerID = _Animator.GetLayerIndex (AnimHashIDs.Layers.LEGS);
	}
	
	public void Walk (float speed, int variation)
	{
		_Animator.SetBool (AnimHashIDs.IS_WALKING_BOOL, speed > 0f);
		_Animator.SetFloat (AnimHashIDs.WALK_SPEED_FLOAT, speed);
		_Animator.SetInt (AnimHashIDs.WALK_VARIATION_INT, variation);
		
		_Animator.SetLayerWeight(_legsLayerID, 1f);
	}
}
~~~~

# FAQ
### What if I have Animator Controllers with parameters that have the same name?
> The system will detect any duplicates and only generate a hash variable for the first parameter with a given name.
