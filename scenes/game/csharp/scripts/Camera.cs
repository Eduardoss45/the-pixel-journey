using Godot;
using System;

public partial class Camera : Camera2D
{
	private Node2D target;

	public override void _Ready()
	{
		GetTarget();
	}

	public override void _Process(double delta)
	{
		if (target != null)
		{
			Position = target.Position;
		}
	}

	private void GetTarget()
	{
		var nodes = GetTree().GetNodesInGroup("Player");

		if (nodes.Count == 0)
		{
			GD.PushError("Player not found");
			return;
		}

		target = nodes[0] as Node2D;

		if (target == null)
		{
			GD.PushError("Player node is not a Node2D");
		}
	}
}