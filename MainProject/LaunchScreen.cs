using GdExtension;
using Godot;

public partial class LaunchScreen : Node2D
{
	[Signal]
	public delegate void MySignalEventHandler();

	[OnReadyNode]
	private Node _hello;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print("Hello Ready!");
		MySignal += () => GD.Print("Hello!");
		EmitSignal(SignalName.MySignal);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) { }

	partial void HelloFrom(string name);
}
